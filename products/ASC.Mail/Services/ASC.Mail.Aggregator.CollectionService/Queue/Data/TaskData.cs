using System.Threading.Tasks;

using ASC.Mail.Models;

namespace ASC.Mail.Aggregator.CollectionService.Queue.Data
{
    public class TaskData
    {
        public MailBoxData Mailbox { get; private set; }

        public Task Task { get; private set; }

        public TaskData(MailBoxData mailBoxData, Task task)
        {
            Mailbox = mailBoxData;
            Task = task;
        }
    }
}
