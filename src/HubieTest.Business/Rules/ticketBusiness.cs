using System;
using System.Collections.Generic;
using HubieTest.Business.Data;
using HubieTest.Dal;

namespace HubieTest.Business
{
    /// <summary>
    /// Ticket business rules. Orchestrates ticketDB and applies the status
    /// transition rules, interaction authorship, etc.
    ///
    /// ========================= CANDIDATE AREA =========================
    /// Implement the rules below. The logged-in user (id/profile/name) is
    /// injected by the handler from the JWT — use it instead of trusting an
    /// id that comes from the request body.
    /// ==================================================================
    /// </summary>
    public class ticketBusiness
    {
        private readonly ITicketDB _db;
        public ticketBusiness() { _db = new ticketDB(); }
        public ticketBusiness(ITicketDB db) { _db = db; }

        // logged-in user context (set by the handler from the token)
        public long loggedUserId { get; set; }
        public string loggedUserName { get; set; }
        public string loggedUserProfile { get; set; }

        public bool hasError { get; set; }
        public string ErrorMessage { get; set; }

        // Valid ticket status values (suggestion).
        public const string STATUS_OPEN = "OPEN";
        public const string STATUS_IN_PROGRESS = "IN_PROGRESS";
        public const string STATUS_ANSWERED = "ANSWERED";
        public const string STATUS_CLOSED = "CLOSED";

        /// <summary>REQUESTER opens a new ticket. Returns the created ticket.</summary>
        public TICKET open(TICKET ticket)
        {
            if (loggedUserProfile != "REQUESTER")
                throw new UnauthorizedAccessException("Only REQUESTER can open a ticket.");

            if (string.IsNullOrWhiteSpace(ticket.TICKET_TITLE))
                throw new ArgumentException("Title is required.");

            if (string.IsNullOrWhiteSpace(ticket.TICKET_DESCRIPTION))
                throw new ArgumentException("Description is required.");

            if (ticket.CATEGORY_ID <= 0)
                throw new ArgumentException("Category is required.");

            ticket.REQUESTER_ID = loggedUserId;
            ticket.REQUESTER_NAME = loggedUserName;
            ticket.TICKET_STATUS = STATUS_OPEN;
            ticket.TICKET_CREATED_DT = DateTime.Now;

            long id = _db.create(ticket);
            ticket.TICKET_ID = id;
            return ticket;
        }

        /// <summary>Lists the tickets of the logged-in requester.</summary>
        public List<TICKET> listMyTickets()
        {
            if (loggedUserProfile != "REQUESTER")
                throw new UnauthorizedAccessException("Only REQUESTER can list their own tickets.");

            return _db.listByRequester(loggedUserId);
        }

        /// <summary>Service queue (AGENT view).</summary>
        public List<TICKET> listQueue(string status)
        {
            if (loggedUserProfile != "AGENT")
                throw new UnauthorizedAccessException("Only AGENT can view the queue.");

            // sanitize: accept only known statuses
            if (!string.IsNullOrEmpty(status) &&
                status != STATUS_OPEN && status != STATUS_IN_PROGRESS &&
                status != STATUS_ANSWERED && status != STATUS_CLOSED)
            {
                status = null;
            }

            return _db.listQueue(status);
        }

        /// <summary>Ticket detail + interactions + attachments (to build the screen).</summary>
        public TICKET get(long ticketId)
        {
            TICKET ticket = _db.get(ticketId);
            if (ticket == null)
                throw new KeyNotFoundException($"Ticket {ticketId} not found.");

            // REQUESTER can only see their own tickets
            if (loggedUserProfile == "REQUESTER" && ticket.REQUESTER_ID != loggedUserId)
                throw new UnauthorizedAccessException("Access denied.");

            return ticket;
        }

        /// <summary>AGENT takes the ticket (status -> IN_PROGRESS).</summary>
        public void assign(long ticketId)
        {
            if (loggedUserProfile != "AGENT")
                throw new UnauthorizedAccessException("Only AGENT can assign a ticket.");

            TICKET ticket = _db.get(ticketId);
            if (ticket == null)
                throw new KeyNotFoundException($"Ticket {ticketId} not found.");

            if (ticket.TICKET_STATUS != STATUS_OPEN)
                throw new InvalidOperationException(
                    $"Only OPEN tickets can be assigned. Current status: {ticket.TICKET_STATUS}");

            ticket.AGENT_ID = loggedUserId;
            ticket.AGENT_NAME = loggedUserName;
            ticket.TICKET_STATUS = STATUS_IN_PROGRESS;
            ticket.TICKET_UPDATED_DT = DateTime.Now;

            _db.update(ticket);
        }

        /// <summary>Changes the ticket status, respecting valid transitions.</summary>
        public void changeStatus(long ticketId, string newStatus)
        {
            TICKET ticket = _db.get(ticketId);
            if (ticket == null)
                throw new KeyNotFoundException($"Ticket {ticketId} not found.");

            // REQUESTER can only see their own tickets
            if (loggedUserProfile == "REQUESTER" && ticket.REQUESTER_ID != loggedUserId)
                throw new UnauthorizedAccessException("Access denied.");

            validateTransition(ticket.TICKET_STATUS, newStatus, loggedUserProfile);

            ticket.TICKET_STATUS = newStatus;
            ticket.TICKET_UPDATED_DT = DateTime.Now;

            if (newStatus == STATUS_CLOSED)
                ticket.TICKET_CLOSED_DT = DateTime.Now;

            _db.update(ticket);
        }

        /// <summary>
        /// Adds a message to the ticket thread. Valid for BOTH profiles
        /// (requester and agent). Set authorship from the logged-in user.
        /// </summary>
        public INTERACTION addInteraction(long ticketId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty.");

            TICKET ticket = _db.get(ticketId);
            if (ticket == null)
                throw new KeyNotFoundException($"Ticket {ticketId} not found.");

            // REQUESTER can only interact with their own tickets
            if (loggedUserProfile == "REQUESTER" && ticket.REQUESTER_ID != loggedUserId)
                throw new UnauthorizedAccessException("Access denied.");

            // Can't interact with closed tickets
            if (ticket.TICKET_STATUS == STATUS_CLOSED)
                throw new InvalidOperationException("Cannot interact with a CLOSED ticket.");

            var interaction = new INTERACTION
            {
                TICKET_ID = ticketId,
                USER_ID = loggedUserId,
                USER_NAME = loggedUserName,
                USER_PROFILE = loggedUserProfile,
                INTERACTION_MESSAGE = message.Trim(),
                INTERACTION_CREATED_DT = DateTime.Now
            };

            return _db.addInteraction(interaction);
        }

        public List<INTERACTION> listInteractions(long ticketId)
        {
            get(ticketId);
            return _db.listInteractions(ticketId);
        }

        /// <summary>Registers an attachment already saved to disk by the upload handler.</summary>
        public ATTACHMENT registerAttachment(ATTACHMENT attachment)
        {
            get(attachment.TICKET_ID);

            attachment.USER_ID = loggedUserId;
            attachment.ATTACHMENT_CREATED_DT = DateTime.Now;

            return _db.addAttachment(attachment);
        }

        public List<ATTACHMENT> listAttachments(long ticketId)
        {
            get(ticketId); // ownership check
            return _db.listAttachments(ticketId);
        }


        /// <summary>
        /// Enforces the status transition table:
        ///   OPEN        → IN_PROGRESS  (AGENT)
        ///   IN_PROGRESS → ANSWERED     (AGENT)
        ///   IN_PROGRESS → CLOSED       (AGENT)
        ///   ANSWERED    → CLOSED       (AGENT | REQUESTER)
        ///   ANSWERED    → IN_PROGRESS  (AGENT — re-open after answer)
        /// </summary>
        private void validateTransition(string current, string next, string profile)
        {
            bool isAgent = profile == "AGENT";
            bool isRequester = profile == "REQUESTER";

            bool allowed = false;

            switch (current)
            {
                case STATUS_OPEN:
                    // assign() handles OPEN → IN_PROGRESS; changeStatus shouldn't reach here
                    allowed = isAgent && next == STATUS_CLOSED;
                    break;

                case STATUS_IN_PROGRESS:
                    allowed = isAgent && (next == STATUS_ANSWERED || next == STATUS_CLOSED);
                    break;

                case STATUS_ANSWERED:
                    allowed = (isAgent || isRequester) && next == STATUS_CLOSED;
                    if (!allowed) allowed = isAgent && next == STATUS_IN_PROGRESS;
                    break;

                case STATUS_CLOSED:
                    allowed = false; // terminal state
                    break;
            }

            if (!allowed)
                throw new InvalidOperationException(
                    $"Transition {current} → {next} is not allowed for profile {profile}.");
        }
    }
}
