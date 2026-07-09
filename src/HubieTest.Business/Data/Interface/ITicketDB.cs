using System.Collections.Generic;
using HubieTest.Dal;

namespace HubieTest.Business.Data
{
    public interface ITicketDB
    {
        long create(TICKET ticket);
        TICKET get(long ticketId);
        List<TICKET> listByRequester(long requesterId);
        List<TICKET> listQueue(string status);
        void update(TICKET ticket);
        INTERACTION addInteraction(INTERACTION interaction);
        List<INTERACTION> listInteractions(long ticketId);
        ATTACHMENT addAttachment(ATTACHMENT attachment);
        List<ATTACHMENT> listAttachments(long ticketId);
    }
}
