/*
 *
 * (c) Copyright Ascensio System Limited 2010-2020
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using System;
using System.Collections.Generic;
using System.Linq;
using ASC.Mail.Core.Engine;
using ASC.Mail.Models;
using ASC.Mail.Enums;
using ASC.Mail.Enums.Filter;
using NUnit.Framework;

namespace ASC.Mail.Aggregator.Tests.Common.Filters
{
    [TestFixture]
    internal class ValidationFiltersTests
    {
        [Test]
        public void ValidateValidFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    },
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.ToOrCc,
                        Operation = ConditionOperationType.NotContains,
                        Value = "toOrcc@example.com"
                    },
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.To,
                        Operation = ConditionOperationType.Matches,
                        Value = "to@example.com"
                    },
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.Cc,
                        Operation = ConditionOperationType.NotMatches,
                        Value = "cc@example.com"
                    },
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.Subject,
                        Operation = ConditionOperationType.Contains,
                        Value = "[TEST]"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{type:5}" // Spam default folder id
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkTag,
                        Data = "111" // Fake tag Id
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        Mailboxes = new[] { 111 },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                },
                Name = "TEST FILTER",
                Position = 1
            };

            MailSieveFilterData validFilter = null;

            Assert.DoesNotThrow(() => { validFilter = FilterEngine.GetValidFilter(filter); });

            Assert.IsNotNull(validFilter);

            Assert.AreEqual(5, validFilter.Conditions.Count);
            Assert.AreEqual(3, validFilter.Actions.Count);
            Assert.AreEqual(1, validFilter.Options.ApplyTo.Folders.Length);
            Assert.AreEqual(1, validFilter.Options.ApplyTo.Mailboxes.Length);
            Assert.AreEqual("TEST FILTER", validFilter.Name);
            Assert.AreEqual(1, validFilter.Position);
        }

        [Test]
        public void ValidateNoOptionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = null
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); }, "No options");
        }

        [Test]
        public void ValidateNoOptionsFolders1Filter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = null,
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); }, "No folders in options");
        }

        [Test]
        public void ValidateNoOptionsFolders2Filter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new int[] { },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); }, "No folders in options");
        }

        [Test]
        public void ValidateNoOptionsNotAceptedFoldersFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Draft, (int)FolderType.Trash, (int)FolderType.Sending, (int)FolderType.UserFolder, 177 },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); }, "Some folder is not accepted in the options");
        }

        [Test]
        public void ValidateNoOptionsMailboxesFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        Mailboxes = null,
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            MailSieveFilterData validaFilter = null;

            Assert.DoesNotThrow(() => { validaFilter = FilterEngine.GetValidFilter(filter); });

            Assert.IsNotNull(validaFilter.Options.ApplyTo.Mailboxes);
            Assert.IsEmpty(validaFilter.Options.ApplyTo.Mailboxes);
        }

        [Test]
        public void ValidateNoActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); }, "No actions");
        }

        [Test]
        public void ValidateDuplicatedActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsImportant
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsImportant
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkTag,
                        Data = "111"
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkTag,
                        Data = "222"
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{type:1}"
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{type:4}"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            MailSieveFilterData validFilter = null;

            Assert.DoesNotThrow(() => { validFilter = FilterEngine.GetValidFilter(filter); });

            Assert.AreEqual(4, validFilter.Actions.Count);
        }

        [Test]
        public void ValidateDeleteForeverAndOtherActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.DeleteForever
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            MailSieveFilterData validFilter = null;

            Assert.DoesNotThrow(() => { validFilter = FilterEngine.GetValidFilter(filter); });

            Assert.AreEqual(1, validFilter.Actions.Count);
            Assert.AreEqual(ActionType.DeleteForever, validFilter.Actions.First().Action);
        }

        [Test]
        public void ValidateInvalidTagDataActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkTag,
                        Data = "fake value"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Not numeric tag id of 'Add tag' action");
        }

        [Test]
        public void ValidateInvalidMoveToJsonDataActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "fake value"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Not json data of 'Move to' action");
        }

        [Test]
        public void ValidateInvalidMoveToEmptyJsonDataActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{}"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Have no values in json data of 'Move to' action");
        }

        [Test]
        public void ValidateInvalidMoveToJsonDataNoTypeActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{userFolderId: 211}"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Have no type value in json data of 'Move to' action");
        }

        [Test]
        public void ValidateInvalidMoveToJsonDataInvalidType1ActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{type: \"fake value\"}"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Not valid type value in json data of 'Move to' action");
        }

        [Test]
        public void ValidateInvalidMoveToJsonDataInvalidType2ActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{type: \"-3\"}"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Not valid type value in json data of 'Move to' action");
        }

        [Test]
        public void ValidateInvalidMoveToJsonDataNoUserFolderIdActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{type: \"6\"}"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Have no userFolderId value in json data of 'Move to' action");
        }

        [Test]
        public void ValidateInvalidMoveToJsonDataInvalidUserFolderId1ActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{type: \"6\", userFolderId: \"fake value\"}"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Have no userFolderId value in json data of 'Move to' action");
        }

        [Test]
        public void ValidateInvalidMoveToJsonDataInvalidUserFolderId2ActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MoveTo,
                        Data = "{type: \"6\", userFolderId: \"-1\"}"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); },
                "Have no userFolderId value in json data of 'Move to' action");
        }

        [Test]
        public void ValidateSetNullDataActionsFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsImportant,
                        Data = "Fake data"
                    },
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead,
                        Data = "Fake data"
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            MailSieveFilterData validFilter = null;

            Assert.DoesNotThrow(() => { validFilter = FilterEngine.GetValidFilter(filter); });

            Assert.IsNull(validFilter.Actions[0].Data);
            Assert.IsNull(validFilter.Actions[1].Data);
        }

        [Test]
        public void ValidateSetLongNameFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                },
                Name = "1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890" // 109 
            };

            MailSieveFilterData validFilter = null;

            Assert.DoesNotThrow(() => { validFilter = FilterEngine.GetValidFilter(filter); });

            Assert.IsNotNull(validFilter);
            Assert.IsNotNull(validFilter.Name);
            Assert.IsNotEmpty(validFilter.Name);
            Assert.AreEqual(64, validFilter.Name.Length);
        }

        [Test]
        public void ValidateSetNegativePositionFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.From,
                        Operation = ConditionOperationType.Contains,
                        Value = "support@example.com"
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                },
                Position = -1
            };

            MailSieveFilterData validFilter = null;

            Assert.DoesNotThrow(() => { validFilter = FilterEngine.GetValidFilter(filter); });

            Assert.IsNotNull(validFilter);
            Assert.AreEqual(0, validFilter.Position);
        }

        [Test]
        public void ValidateSetLongConditionValueFilter()
        {
            var filter = new MailSieveFilterData
            {
                Conditions = new List<MailSieveFilterConditionData>
                {
                    new MailSieveFilterConditionData
                    {
                        Key = ConditionKeyType.Subject,
                        Operation = ConditionOperationType.Contains,
                        Value = "1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890" // 219
                    }
                },
                Actions = new List<MailSieveFilterActionData>
                {
                    new MailSieveFilterActionData
                    {
                        Action = ActionType.MarkAsRead
                    }
                },
                Options = new MailSieveFilterOptionsData
                {
                    MatchMultiConditions = MatchMultiConditionsType.MatchAtLeastOne,
                    ApplyTo = new MailSieveFilterOptionsApplyToData
                    {
                        Folders = new[] { (int)FolderType.Inbox },
                        WithAttachments = ApplyToAttachmentsType.WithAndWithoutAttachments
                    }
                }
            };

            Assert.Throws<ArgumentException>(() => { FilterEngine.GetValidFilter(filter); }, "Too long condition value (limit is 200)");
        }
    }
}
