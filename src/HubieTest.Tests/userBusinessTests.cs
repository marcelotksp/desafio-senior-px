using HubieTest.Business;
using HubieTest.Business.Data;
using HubieTest.Business.Security;
using HubieTest.Dal;
using Moq;
using NUnit.Framework;

namespace HubieTest.Tests
{
    [TestFixture]
    public class userBusinessTests
    {
        private static APP_USER FakeUser(string login = "marcelo", string profile = "REQUESTER")
        {
            return new APP_USER
            {
                USER_ID       = 1,
                USER_LOGIN    = login,
                USER_NAME     = "Marcelo",
                USER_PROFILE  = profile,
                USER_EMAIL    = "marcelo@hubie.com",
                USER_ACTIVE   = true,
                USER_PASSWORD = SecurityHelper.HashPassword("secret")
            };
        }

        [Test]
        public void auth_ReturnsUserNotFound_WhenLoginDoesNotExist()
        {
            var db = new Mock<IUserDB>();
            db.Setup(d => d.getByLogin("ghost")).Returns((APP_USER)null);

            var result = new userBusiness(db.Object).auth("ghost", "any");

            Assert.That(result.STATUS, Is.EqualTo("USER_NOT_FOUND"));
            Assert.That(result.TOKEN,  Is.Null);
        }

        [Test]
        public void auth_ReturnsInvalidPassword_WhenPasswordWrong()
        {
            var db = new Mock<IUserDB>();
            db.Setup(d => d.getByLogin("marcelo")).Returns(FakeUser());

            var result = new userBusiness(db.Object).auth("marcelo", "wrong");

            Assert.That(result.STATUS, Is.EqualTo("INVALID_PASSWORD"));
            Assert.That(result.TOKEN,  Is.Null);
        }

        [Test]
        public void auth_ReturnsOkWithToken_WhenCredentialsCorrect()
        {
            var db = new Mock<IUserDB>();
            db.Setup(d => d.getByLogin("marcelo")).Returns(FakeUser());

            var result = new userBusiness(db.Object).auth("marcelo", "secret");

            Assert.That(result.STATUS,       Is.EqualTo("OK"));
            Assert.That(result.TOKEN,        Is.Not.Null);
            Assert.That(result.USER_ID,      Is.EqualTo(1L));
            Assert.That(result.USER_LOGIN,   Is.EqualTo("marcelo"));
            Assert.That(result.USER_PROFILE, Is.EqualTo("REQUESTER"));
        }

        [Test]
        public void auth_ReturnedTokenIsValidJwt()
        {
            var db = new Mock<IUserDB>();
            db.Setup(d => d.getByLogin("marcelo")).Returns(FakeUser());

            var result = new userBusiness(db.Object).auth("marcelo", "secret");

            TokenClaims claims;
            bool valid = SecurityHelper.TryValidate(result.TOKEN, out claims);

            Assert.That(valid,           Is.True);
            Assert.That(claims.UserId,   Is.EqualTo(1L));
            Assert.That(claims.Login,    Is.EqualTo("marcelo"));
            Assert.That(claims.Profile,  Is.EqualTo("REQUESTER"));
        }
    }
}
