using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using HubieTest.Dal;

namespace HubieTest.Business.Data
{
    /// <summary>
    /// Data access for TICKET, INTERACTION and ATTACHMENT.
    ///
    /// ========================= CANDIDATE AREA =========================
    /// Implement the methods below following the pattern shown in
    /// categoryDB.cs (open a DbContext in a using, turn off proxy/lazy,
    /// use EntityState for create/update). Feel free to adjust signatures
    /// if you think it is better — just explain your choices in the PR.
    /// ==================================================================
    /// </summary>
    public class ticketDB
    {
        // ---------- TICKET ----------

        /// <summary>Inserts a new ticket and returns the generated Id (Identity).</summary>
        public long create(TICKET ticket)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Entry(ticket).State = EntityState.Added;
                db.SaveChanges();
                return ticket.TICKET_ID;
            }
        }

        /// <summary>Returns a ticket by id (or null).</summary>
        public TICKET get(long ticketId)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                return db.TICKETS
                         .AsNoTracking()
                         .FirstOrDefault(t => t.TICKET_ID == ticketId);
            }
        }

        /// <summary>Lists the tickets opened by a requester (most recent first).</summary>
        public List<TICKET> listByRequester(long requesterId)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                return db.TICKETS
                         .AsNoTracking()
                         .Where(t => t.REQUESTER_ID == requesterId)
                         .OrderByDescending(t => t.TICKET_CREATED_DT)
                         .ToList();
            }
        }

        /// <summary>
        /// Agent queue. If <paramref name="status"/> is null/empty, return every
        /// ticket that is not closed yet.
        /// </summary>
        public List<TICKET> listQueue(string status)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;

                IQueryable<TICKET> query = db.TICKETS.AsNoTracking();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(t => t.TICKET_STATUS == status);
                else
                    query = query.Where(t => t.TICKET_STATUS != "CLOSED");

                return query.OrderByDescending(t => t.TICKET_CREATED_DT).ToList();
            }
        }

        /// <summary>Updates an existing ticket (status, agent, dates, etc.).</summary>
        public void update(TICKET ticket)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        // ---------- INTERACTION ----------

        public INTERACTION addInteraction(INTERACTION interaction)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Entry(interaction).State = EntityState.Added;
                db.SaveChanges();
                return interaction;
            }
        }

        public List<INTERACTION> listInteractions(long ticketId)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                return db.INTERACTIONS
                         .AsNoTracking()
                         .Where(i => i.TICKET_ID == ticketId)
                         .OrderBy(i => i.INTERACTION_CREATED_DT)
                         .ToList();
            }
        }

        // ---------- ATTACHMENT ----------

        public ATTACHMENT addAttachment(ATTACHMENT attachment)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                db.Entry(attachment).State = EntityState.Added;
                db.SaveChanges();
                return attachment;
            }
        }

        public List<ATTACHMENT> listAttachments(long ticketId)
        {
            using (var db = new HubieContext())
            {
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;
                return db.ATTACHMENTS
                         .AsNoTracking()
                         .Where(a => a.TICKET_ID == ticketId)
                         .OrderByDescending(a => a.ATTACHMENT_CREATED_DT)
                         .ToList();
            }
        }
    }
}
