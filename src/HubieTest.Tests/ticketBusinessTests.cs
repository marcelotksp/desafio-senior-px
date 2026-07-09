using System;
using System.Collections.Generic;
using HubieTest.Business;
using HubieTest.Business.Data;
using HubieTest.Dal;
using Moq;
using NUnit.Framework;

namespace HubieTest.Tests
{
    [TestFixture]
    public class ticketBusinessTests
    {
        private static ticketBusiness MakeRequester(Mock<ITicketDB> db, long userId = 1, string name = "Marcelo")
        {
            return new ticketBusiness(db.Object)
            {
                loggedUserId      = userId,
                loggedUserName    = name,
                loggedUserProfile = "REQUESTER"
            };
        }

        private static ticketBusiness MakeAgent(Mock<ITicketDB> db, long userId = 2, string name = "Takashi")
        {
            return new ticketBusiness(db.Object)
            {
                loggedUserId      = userId,
                loggedUserName    = name,
                loggedUserProfile = "AGENT"
            };
        }

        private static TICKET Ticket(long id, string status, long requesterId = 1)
        {
            return new TICKET
            {
                TICKET_ID     = id,
                TICKET_TITLE  = "Test ticket",
                TICKET_STATUS = status,
                CATEGORY_ID   = 1,
                REQUESTER_ID  = requesterId
            };
        }


        [Test]
        public void open_RejectsAgent()
        {
            var db  = new Mock<ITicketDB>();
            var sut = MakeAgent(db);
            Assert.Throws<UnauthorizedAccessException>(() =>
                sut.open(new TICKET { TICKET_TITLE = "t", CATEGORY_ID = 1 }));
        }

        [Test]
        public void open_RejectsEmptyTitle()
        {
            var db  = new Mock<ITicketDB>();
            var sut = MakeRequester(db);
            Assert.Throws<ArgumentException>(() =>
                sut.open(new TICKET { TICKET_TITLE = "   ", CATEGORY_ID = 1 }));
        }

        [Test]
        public void open_RejectsMissingCategory()
        {
            var db  = new Mock<ITicketDB>();
            var sut = MakeRequester(db);
            Assert.Throws<ArgumentException>(() =>
                sut.open(new TICKET { TICKET_TITLE = "Help me", CATEGORY_ID = 0 }));
        }

        [Test]
        public void open_SetsFieldsAndCallsCreate()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.create(It.IsAny<TICKET>())).Returns(42L);

            var sut    = MakeRequester(db, userId: 7, name: "Carol");
            var result = sut.open(new TICKET { TICKET_TITLE = "Need help", CATEGORY_ID = 3, TICKET_DESCRIPTION = "Testing" });

            Assert.That(result.REQUESTER_ID,   Is.EqualTo(7L));
            Assert.That(result.REQUESTER_NAME, Is.EqualTo("Carol"));
            Assert.That(result.TICKET_STATUS,  Is.EqualTo("OPEN"));
            db.Verify(d => d.create(It.IsAny<TICKET>()), Times.Once);
        }


        [Test]
        public void listMyTickets_RejectsAgent()
        {
            var db  = new Mock<ITicketDB>();
            var sut = MakeAgent(db);
            Assert.Throws<UnauthorizedAccessException>(() => sut.listMyTickets());
        }

        [Test]
        public void listMyTickets_CallsDbWithLoggedUserId()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.listByRequester(5L)).Returns(new List<TICKET>());

            var sut = MakeRequester(db, userId: 5);
            sut.listMyTickets();

            db.Verify(d => d.listByRequester(5L), Times.Once);
        }


        [Test]
        public void listQueue_RejectsRequester()
        {
            var db  = new Mock<ITicketDB>();
            var sut = MakeRequester(db);
            Assert.Throws<UnauthorizedAccessException>(() => sut.listQueue(null));
        }

        [Test]
        public void listQueue_PassesValidStatus()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.listQueue("OPEN")).Returns(new List<TICKET>());

            var sut = MakeAgent(db);
            sut.listQueue("OPEN");

            db.Verify(d => d.listQueue("OPEN"), Times.Once);
        }

        [Test]
        public void listQueue_SanitizesInvalidStatus()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.listQueue(null)).Returns(new List<TICKET>());

            var sut = MakeAgent(db);
            sut.listQueue("BOGUS");

            db.Verify(d => d.listQueue(null), Times.Once);
        }


        [Test]
        public void get_ThrowsWhenNotFound()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(99L)).Returns((TICKET)null);

            var sut = MakeAgent(db);
            Assert.Throws<KeyNotFoundException>(() => sut.get(99L));
        }

        [Test]
        public void get_RequesterCannotSeeOtherTicket()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN", requesterId: 99));

            var sut = MakeRequester(db, userId: 1);
            Assert.Throws<UnauthorizedAccessException>(() => sut.get(1L));
        }

        [Test]
        public void get_RequesterCanSeeOwnTicket()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN", requesterId: 1));

            var sut    = MakeRequester(db, userId: 1);
            var result = sut.get(1L);

            Assert.That(result.TICKET_ID, Is.EqualTo(1L));
        }

        [Test]
        public void get_AgentCanSeeAnyTicket()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN", requesterId: 99));

            var sut    = MakeAgent(db);
            var result = sut.get(1L);

            Assert.That(result, Is.Not.Null);
        }


        [Test]
        public void assign_RejectsRequester()
        {
            var db  = new Mock<ITicketDB>();
            var sut = MakeRequester(db);
            Assert.Throws<UnauthorizedAccessException>(() => sut.assign(1L));
        }

        [Test]
        public void assign_ThrowsWhenNotFound()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns((TICKET)null);

            var sut = MakeAgent(db);
            Assert.Throws<KeyNotFoundException>(() => sut.assign(1L));
        }

        [Test]
        public void assign_ThrowsWhenNotOpen()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "IN_PROGRESS"));

            var sut = MakeAgent(db);
            Assert.Throws<InvalidOperationException>(() => sut.assign(1L));
        }

        [Test]
        public void assign_SetsAgentAndStatus()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN"));

            TICKET saved = null;
            db.Setup(d => d.update(It.IsAny<TICKET>()))
              .Callback<TICKET>(t => saved = t);

            var sut = MakeAgent(db, userId: 2, name: "Bob");
            sut.assign(1L);

            Assert.That(saved.AGENT_ID,      Is.EqualTo(2L));
            Assert.That(saved.AGENT_NAME,    Is.EqualTo("Bob"));
            Assert.That(saved.TICKET_STATUS, Is.EqualTo("IN_PROGRESS"));
        }


        [TestCase("OPEN",        "CLOSED",      "AGENT")]
        [TestCase("IN_PROGRESS", "ANSWERED",    "AGENT")]
        [TestCase("IN_PROGRESS", "CLOSED",      "AGENT")]
        [TestCase("ANSWERED",    "CLOSED",      "AGENT")]
        [TestCase("ANSWERED",    "CLOSED",      "REQUESTER")]
        [TestCase("ANSWERED",    "IN_PROGRESS", "AGENT")]
        public void changeStatus_AllowsValidTransition(string from, string to, string profile)
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, from, requesterId: 1));
            db.Setup(d => d.update(It.IsAny<TICKET>()));

            var sut = new ticketBusiness(db.Object)
            {
                loggedUserId      = 1,
                loggedUserName    = "User",
                loggedUserProfile = profile
            };

            Assert.DoesNotThrow(() => sut.changeStatus(1L, to));
            db.Verify(d => d.update(It.IsAny<TICKET>()), Times.Once);
        }


        [TestCase("OPEN",        "ANSWERED",    "AGENT")]
        [TestCase("OPEN",        "CLOSED",      "REQUESTER")]
        [TestCase("IN_PROGRESS", "ANSWERED",    "REQUESTER")]
        [TestCase("ANSWERED",    "IN_PROGRESS", "REQUESTER")]
        [TestCase("CLOSED",      "OPEN",        "AGENT")]
        [TestCase("CLOSED",      "IN_PROGRESS", "AGENT")]
        public void changeStatus_BlocksInvalidTransition(string from, string to, string profile)
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, from, requesterId: 1));

            var sut = new ticketBusiness(db.Object)
            {
                loggedUserId      = 1,
                loggedUserName    = "User",
                loggedUserProfile = profile
            };

            Assert.Throws<InvalidOperationException>(() => sut.changeStatus(1L, to));
        }

        [Test]
        public void changeStatus_ThrowsWhenNotFound()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns((TICKET)null);

            var sut = MakeAgent(db);
            Assert.Throws<KeyNotFoundException>(() => sut.changeStatus(1L, "CLOSED"));
        }

        [Test]
        public void changeStatus_RequesterCannotAccessOtherTicket()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "ANSWERED", requesterId: 99));

            var sut = MakeRequester(db, userId: 1);
            Assert.Throws<UnauthorizedAccessException>(() => sut.changeStatus(1L, "CLOSED"));
        }

        [Test]
        public void changeStatus_SetsCLOSEDDate()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "IN_PROGRESS"));

            TICKET saved = null;
            db.Setup(d => d.update(It.IsAny<TICKET>()))
              .Callback<TICKET>(t => saved = t);

            var sut = MakeAgent(db);
            sut.changeStatus(1L, "CLOSED");

            Assert.That(saved.TICKET_CLOSED_DT, Is.Not.Null);
        }


        [Test]
        public void addInteraction_ThrowsOnEmptyMessage()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN"));

            var sut = MakeRequester(db, userId: 1);
            Assert.Throws<ArgumentException>(() => sut.addInteraction(1L, "   "));
        }

        [Test]
        public void addInteraction_ThrowsWhenTicketNotFound()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns((TICKET)null);

            var sut = MakeRequester(db, userId: 1);
            Assert.Throws<KeyNotFoundException>(() => sut.addInteraction(1L, "Hello"));
        }

        [Test]
        public void addInteraction_ThrowsWhenClosed()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "CLOSED", requesterId: 1));

            var sut = MakeRequester(db, userId: 1);
            Assert.Throws<InvalidOperationException>(() => sut.addInteraction(1L, "Hello"));
        }

        [Test]
        public void addInteraction_ThrowsWhenRequesterAccessesOtherTicket()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN", requesterId: 99));

            var sut = MakeRequester(db, userId: 1);
            Assert.Throws<UnauthorizedAccessException>(() => sut.addInteraction(1L, "Hello"));
        }

        [Test]
        public void addInteraction_SetsAuthorshipFromJwtAndTrimsMessage()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN", requesterId: 1));

            INTERACTION saved = null;
            db.Setup(d => d.addInteraction(It.IsAny<INTERACTION>()))
              .Callback<INTERACTION>(i => saved = i)
              .Returns<INTERACTION>(i => i);

            var sut = MakeRequester(db, userId: 1, name: "Marcelo");
            sut.addInteraction(1L, "  Hello!  ");

            Assert.That(saved.USER_ID,               Is.EqualTo(1L));
            Assert.That(saved.USER_NAME,             Is.EqualTo("Marcelo"));
            Assert.That(saved.USER_PROFILE,          Is.EqualTo("REQUESTER"));
            Assert.That(saved.INTERACTION_MESSAGE,   Is.EqualTo("Hello!"));
        }


        [Test]
        public void listInteractions_AppliesOwnershipCheck()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN", requesterId: 99));

            var sut = MakeRequester(db, userId: 1);
            Assert.Throws<UnauthorizedAccessException>(() => sut.listInteractions(1L));
        }


        [Test]
        public void registerAttachment_SetsUserIdAndDate()
        {
            var db = new Mock<ITicketDB>();
            db.Setup(d => d.get(1L)).Returns(Ticket(1, "OPEN", requesterId: 1));

            ATTACHMENT saved = null;
            db.Setup(d => d.addAttachment(It.IsAny<ATTACHMENT>()))
              .Callback<ATTACHMENT>(a => saved = a)
              .Returns<ATTACHMENT>(a => a);

            var sut = MakeRequester(db, userId: 1);
            sut.registerAttachment(new ATTACHMENT { TICKET_ID = 1, ATTACHMENT_NAME = "doc.pdf" });

            Assert.That(saved.USER_ID,                Is.EqualTo(1L));
            Assert.That(saved.ATTACHMENT_CREATED_DT, Is.Not.Null);
        }
    }
}
