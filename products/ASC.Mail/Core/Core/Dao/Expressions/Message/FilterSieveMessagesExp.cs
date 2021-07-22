using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ASC.ElasticSearch;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Enums.Filter;
using ASC.Mail.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ASC.Mail.Core.Dao.Expressions.Message
{
    public class FilterSieveMessagesExp : IMessagesExp
    {
        public List<int> Ids { get; private set; }
        public MailSieveFilterData Filter { get; private set; }
        public int Tenant { get; private set; }
        public string User { get; private set; }

        public string OrderBy
        {
            get { return "date_sent"; }
        }

        public bool? OrderAsc
        {
            get
            {
                return null;
            }
        }

        public int? StartIndex { get; set; }
        public int? Limit { get; set; }
        public FactoryIndexer<MailMail> FactoryIndexer { get; }
        public IServiceProvider ServiceProvider { get; }

        public List<int> TagIds
        {
            get { return null; }
        }

        public int? UserFolderId
        {
            get { return null; }
        }

        public FilterSieveMessagesExp(List<int> ids, int tenant, string user, MailSieveFilterData filter, int page,
            int pageSize, 
            FactoryIndexer<MailMail> factoryIndexer, 
            IServiceProvider serviceProvider)
        {
            Filter = filter;
            Tenant = tenant;
            User = user;
            FactoryIndexer = factoryIndexer;
            ServiceProvider = serviceProvider;

            if (ids.Any())
            {
                Ids = ids.Skip(page*pageSize).Take(pageSize).ToList();
                return;
            }

            StartIndex = page*pageSize;
            Limit = pageSize;
        }

        public virtual Expression<Func<MailMail, bool>> GetExpression()
        {
            Expression<Func<MailMail, bool>> filterExp = m => 
                m.TenantId == Tenant && m.IdUser == User && m.IsRemoved == false;

            var t = ServiceProvider.GetService<MailMail>();
            if (!FactoryIndexer.Support(t))
            {
                Expression<Func<MailMail, bool>> getConditionExp(MailSieveFilterConditionData c)
                {
                    Expression<Func<MailMail, bool>> e = m => true;

                    switch (c.Operation)
                    {
                        case ConditionOperationType.Matches:
                            e = c.Key switch
                            {
                                ConditionKeyType.From => m => m.FromText == c.Value,
                                ConditionKeyType.To => m => m.ToText == c.Value,
                                ConditionKeyType.Cc => m => m.Cc == c.Value,
                                ConditionKeyType.Subject => m => m.Subject == c.Value,
                                ConditionKeyType.ToOrCc => m => m.ToText == c.Value || m.Cc == c.Value,
                                _ => throw new ArgumentOutOfRangeException("c", c, null),
                            };

                            break;
                        case ConditionOperationType.Contains:
                            e = c.Key switch
                            {
                                ConditionKeyType.From => m => m.FromText.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                ConditionKeyType.To => m => m.ToText.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                ConditionKeyType.Cc => m => m.Cc.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                ConditionKeyType.Subject => m => m.Subject.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                ConditionKeyType.ToOrCc => m => m.ToText.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase)
                                                             || m.Cc.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                _ => throw new ArgumentOutOfRangeException("c", c, null),
                            };
                            break;
                        case ConditionOperationType.NotMatches:
                            e = c.Key switch
                            {
                                ConditionKeyType.From => m => m.FromText != c.Value,
                                ConditionKeyType.To => m => m.ToText != c.Value,
                                ConditionKeyType.Cc => m => m.Cc != c.Value,
                                ConditionKeyType.Subject => m => m.Subject != c.Value,
                                ConditionKeyType.ToOrCc => m => m.ToText != c.Value && m.Cc != c.Value,
                                _ => throw new ArgumentOutOfRangeException("c", c, null),
                            };
                            break;
                        case ConditionOperationType.NotContains:
                            e = c.Key switch
                            {
                                ConditionKeyType.From => m => !m.FromText.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                ConditionKeyType.To => m => !m.ToText.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                ConditionKeyType.Cc => m => !m.Cc.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                ConditionKeyType.Subject => m => !m.Subject.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                ConditionKeyType.ToOrCc => m => !m.ToText.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase)
                                                             && !m.Cc.Contains(c.Value, StringComparison.InvariantCultureIgnoreCase),
                                _ => throw new ArgumentOutOfRangeException("c", c, null),
                            };
                            break;
                    }

                    return e;
                }

                if (Filter.Conditions != null && Filter.Conditions.Any())
                {
                    Expression<Func<MailMail, bool>> cExp = null;

                    foreach (var c in Filter.Conditions)
                    {
                        switch (Filter.Options.MatchMultiConditions)
                        {
                            case MatchMultiConditionsType.MatchAll:
                            case MatchMultiConditionsType.None:
                                cExp = cExp == null ? getConditionExp(c) : cExp.And(getConditionExp(c));
                                break;
                            case MatchMultiConditionsType.MatchAtLeastOne:
                                cExp = cExp == null ? getConditionExp(c) : cExp.Or(getConditionExp(c));
                                break;
                        }
                    }

                    filterExp = filterExp.And(cExp);
                }
            }

            if (Ids != null && Ids.Any())
            {
                filterExp = filterExp.And(m => Ids.Contains(m.Id));
            }

            if (Filter.Options.ApplyTo.Folders.Any())
            {
                filterExp = filterExp.And(m => Filter.Options.ApplyTo.Folders.Contains(m.Folder));
            }

            if (Filter.Options.ApplyTo.Mailboxes.Any())
            {
                filterExp = filterExp.And(m => Filter.Options.ApplyTo.Mailboxes.Contains(m.IdMailbox));
            }

            switch (Filter.Options.ApplyTo.WithAttachments)
            {
                case ApplyToAttachmentsType.WithAttachments:
                    filterExp = filterExp.And(m => m.AttachmentsCount > 0);
                    break;
                case ApplyToAttachmentsType.WithoutAttachments:
                    filterExp = filterExp.And(m => m.AttachmentsCount == 0);
                    break;
                case ApplyToAttachmentsType.WithAndWithoutAttachments:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return filterExp;
        }

        public static bool TryGetFullTextSearchIds(
            FactoryIndexer<MailMail> factoryIndexer, 
            IServiceProvider serviceProvider, 
            MailSieveFilterData filter, string user, out List<int> ids, out long total)
        {
            ids = new List<int>();

            var t = serviceProvider.GetService<MailMail>();
            if (!factoryIndexer.Support(t))
            {
                total = 0;
                return false;
            }

            var userId = new Guid(user);

            static Expression<Func<MailMail, object>> getExp(ConditionKeyType c)
            {
                return c switch
                {
                    ConditionKeyType.From => w => w.FromText,
                    ConditionKeyType.To => w => w.ToText,
                    ConditionKeyType.Cc => w => w.Cc,
                    ConditionKeyType.Subject => w => w.Subject,
                    _ => throw new ArgumentOutOfRangeException("c", c, null),
                };
            }

            static string getValue(MailSieveFilterConditionData c)
            {
                return c.Operation == ConditionOperationType.Matches || c.Operation == ConditionOperationType.NotMatches
                    ? string.Format("\"{0}\"", c.Value)
                    : c.Value;
            }

            Func<MailSieveFilterConditionData, Selector<MailMail>> setSelector = (c) =>
            {
                Selector<MailMail> sel;

                var value = getValue(c);

                if (c.Key == ConditionKeyType.ToOrCc)
                {
                    sel = new Selector<MailMail>(serviceProvider).Or(
                        s => s.Match(w => w.ToText, value), 
                        s => s.Match(w => w.Cc, value));
                }
                else
                {
                    sel = new Selector<MailMail>(serviceProvider).Match(getExp(c.Key), value);
                }

                if (c.Operation == ConditionOperationType.NotMatches ||
                    c.Operation == ConditionOperationType.NotContains)
                {
                    return new Selector<MailMail>(serviceProvider).Not(s => sel);
                }

                return sel;
            };

            var selector = new Selector<MailMail>(serviceProvider);

            foreach (var c in filter.Conditions)
            {
                if (filter.Options.MatchMultiConditions == MatchMultiConditionsType.MatchAll ||
                    filter.Options.MatchMultiConditions == MatchMultiConditionsType.None)
                {
                    selector &= setSelector(c);
                }
                else
                {
                    selector |= setSelector(c);
                }
            }

            if (filter.Options.ApplyTo.Folders.Any())
            {
                selector.In(r => r.Folder, filter.Options.ApplyTo.Folders);
            }

            if (filter.Options.ApplyTo.Mailboxes.Any())
            {
                selector.In(r => r.IdMailbox, filter.Options.ApplyTo.Mailboxes);
            }

            if (filter.Options.ApplyTo.WithAttachments != ApplyToAttachmentsType.WithAndWithoutAttachments)
            {
                selector.Where(r => r.HasAttachments,
                    filter.Options.ApplyTo.WithAttachments == ApplyToAttachmentsType.WithAttachments);
            }

            selector
                .Where(r => r.IdUser, userId.ToString())
                .Sort(r => r.DateSent, true);

            if (!factoryIndexer.TrySelectIds(s => selector, out List<int> mailIds, out total))
                return false;

            ids = mailIds;

            return true;
        }
    }
}