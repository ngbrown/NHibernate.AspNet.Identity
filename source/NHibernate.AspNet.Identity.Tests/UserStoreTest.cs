﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Transactions;
using Microsoft.AspNet.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.AspNet.Identity.Tests.Models;
using NHibernate.Linq;

namespace NHibernate.AspNet.Identity.Tests
{
    [TestClass]
    public class UserStoreTest
    {
        ISession _session;

        [TestInitialize]
        public void Initialize()
        {
            var factory = SessionFactoryProvider.Instance.SessionFactory;
            this._session = factory.OpenSession();
            SessionFactoryProvider.Instance.BuildSchema();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this._session.Close();
        }

        [TestMethod]
        public void WhenHaveNoUser()
        {
            var login = new UserLoginInfo("ProviderTest", "ProviderKey");
            var store = new UserStore<IdentityUser>(this._session);
            var user = store.FindAsync(login).Result;

            Assert.IsNull(user);
        }

        [TestMethod]
        public void WhenAddLoginAsync()
        {
            var user = new IdentityUser("Lukz");
            var login = new UserLoginInfo("ProviderTest02", "ProviderKey02");
            var store = new UserStore<IdentityUser>(this._session);
            var result = store.AddLoginAsync(user, login);

            var actual = this._session.Query<IdentityUser>().FirstOrDefault(x => x.UserName == user.UserName);
            var userStored = store.FindAsync(login).Result;

            Assert.IsNull(result.Exception);
            Assert.IsNotNull(actual);
            Assert.AreEqual(user.UserName, actual.UserName);
            Assert.AreEqual(user.UserName, userStored.UserName);
        }

        [TestMethod]
        public void WhenRemoveLoginAsync()
        {
            var user = new IdentityUser("Lukz 03");
            var login = new UserLoginInfo("ProviderTest03", "ProviderKey03");
            var store = new UserStore<IdentityUser>(this._session);
            store.AddLoginAsync(user, login);

            Assert.IsTrue(user.Logins.Any());

            var result = store.RemoveLoginAsync(user, login);

            var actual = this._session.Query<IdentityUser>().FirstOrDefault(x => x.UserName == user.UserName);
            Assert.IsNull(result.Exception);
            Assert.IsFalse(actual.Logins.Any());
        }

        [TestMethod]
        public void WhenCeateUserAsync()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this._session));

            var user = new ApplicationUser() { UserName = "RealUserName" };

            using (var transaction = new TransactionScope())
            {
                var result = userManager.Create(user, "RealPassword");
                transaction.Complete();
                Assert.IsTrue(result.Succeeded);
            }

            var actual = this._session.Query<ApplicationUser>().FirstOrDefault(x => x.UserName == user.UserName);

            Assert.IsNotNull(actual);
            Assert.AreEqual(user.UserName, actual.UserName);
        }

        [TestMethod]
        public void GivenHaveRoles_WhenDeleteUser_ThenDeletingCausesNoCascade()
        {
            var user = new IdentityUser("Lukz 04");
            var role = new IdentityRole("ADM");
            var store = new UserStore<IdentityUser>(this._session);
            var roleStore = new RoleStore<IdentityRole>(this._session);

            roleStore.CreateAsync(role);
            store.CreateAsync(user);
            store.AddToRoleAsync(user, "ADM");

            Assert.IsTrue(this._session.Query<IdentityRole>().Any(x => x.Name == "ADM"));
            Assert.IsTrue(this._session.Query<IdentityUser>().Any(x => x.UserName == "Lukz 04"));

            var result = store.DeleteAsync(user);

            Assert.IsNull(result.Exception);
            Assert.IsFalse(this._session.Query<IdentityUser>().Any(x => x.UserName == "Lukz 04"));
            Assert.IsTrue(this._session.Query<IdentityRole>().Any(x => x.Name == "ADM"));
        }

        [TestMethod]
        public void GetAllUsers()
        {
            var user1 = new IdentityUser("Lukz 04");
            var user2 = new IdentityUser("Moa 01");
            var user3 = new IdentityUser("Win 02");
            var user4 = new IdentityUser("Andre 03");
            var role = new IdentityRole("ADM");
            var store = new UserStore<IdentityUser>(this._session);
            var roleStore = new RoleStore<IdentityRole>(this._session);

            roleStore.CreateAsync(role);
            store.CreateAsync(user1);
            store.CreateAsync(user2);
            store.CreateAsync(user3);
            store.CreateAsync(user4);
            store.AddToRoleAsync(user1, "ADM");
            store.AddToRoleAsync(user2, "ADM");
            store.AddToRoleAsync(user3, "ADM");
            store.AddToRoleAsync(user4, "ADM");

            Assert.IsTrue(this._session.Query<IdentityRole>().Any(x => x.Name == "ADM"));
            Assert.IsTrue(this._session.Query<IdentityUser>().Any(x => x.UserName == "Lukz 04"));

            Assert.IsTrue(this._session.Query<IdentityUser>().Any(x => x.UserName == "Andre 03"));

            var resul = store.Users;

            Assert.AreEqual(4, resul.Count());
        }

        [TestMethod]
        public void GetAllRoles()
        {
            var user1 = new IdentityUser("Lukz 04");
            var user2 = new IdentityUser("Moa 01");
            var user3 = new IdentityUser("Win 02");
            var user4 = new IdentityUser("Andre 03");
            var role = new IdentityRole("ADM");
            var role2 = new IdentityRole("USR");
            var store = new UserStore<IdentityUser>(this._session);
            var roleStore = new RoleStore<IdentityRole>(this._session);

            roleStore.CreateAsync(role);
            roleStore.CreateAsync(role2);
            store.CreateAsync(user1);
            store.CreateAsync(user2);
            store.CreateAsync(user3);
            store.CreateAsync(user4);
            store.AddToRoleAsync(user1, "ADM");
            store.AddToRoleAsync(user2, "ADM");
            store.AddToRoleAsync(user3, "ADM");
            store.AddToRoleAsync(user4, "ADM");
            store.AddToRoleAsync(user1, "USR");
            store.AddToRoleAsync(user4, "USR");

            Assert.IsTrue(this._session.Query<IdentityRole>().Any(x => x.Name == "ADM"));
            Assert.IsTrue(this._session.Query<IdentityUser>().Any(x => x.UserName == "Lukz 04"));

            Assert.IsTrue(this._session.Query<IdentityUser>().Any(x => x.UserName == "Andre 03"));

            var resul = roleStore.Roles;

            Assert.AreEqual(2, resul.Count());
        }

        [TestMethod]
        public void LockoutAccount()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this._session));
            userManager.MaxFailedAccessAttemptsBeforeLockout = 3;
            userManager.UserLockoutEnabledByDefault = true;
            userManager.DefaultAccountLockoutTimeSpan = new TimeSpan(0, 10, 0);
            userManager.Create(new ApplicationUser() { UserName = "test", LockoutEnabled = true }, "Welcome");
            var user = userManager.Find("test", "Welcome");
            Assert.AreEqual(0, userManager.GetAccessFailedCount(user.Id));
            userManager.AccessFailed(user.Id);
            Assert.AreEqual(1, userManager.GetAccessFailedCount(user.Id));
            userManager.AccessFailed(user.Id);
            Assert.AreEqual(2, userManager.GetAccessFailedCount(user.Id));
            userManager.AccessFailed(user.Id);
            Assert.IsTrue(userManager.IsLockedOut(user.Id));
        }

        [TestMethod]
        public void FindByName()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this._session));
            userManager.Create(new ApplicationUser() { UserName = "test", Email = "aaa@bbb.com", EmailConfirmed = true }, "Welcome");
            var x = userManager.FindByName("tEsT");
            Assert.IsNotNull(x);
            Assert.IsTrue(userManager.IsEmailConfirmed(x.Id));
        }

        [TestMethod]
        public void FindByEmail()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this._session));
            userManager.Create(new ApplicationUser() { UserName = "test", Email = "aaa@bbb.com", EmailConfirmed = true }, "Welcome");
            var x = userManager.FindByEmail("AaA@bBb.com");
            Assert.IsNotNull(x);
            Assert.IsTrue(userManager.IsEmailConfirmed(x.Id));
        }

        [TestMethod]
        public void AddClaim()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this._session));
            userManager.Create(new ApplicationUser() { UserName = "test", Email = "aaa@bbb.com", EmailConfirmed = true }, "Welcome");
            var x = userManager.FindByEmail("aaa@bbb.com");
            userManager.AddClaim(x.Id, new Claim(ClaimTypes.Role, "Admin"));
            Assert.AreEqual(1, userManager.GetClaims(x.Id).Count());
        }

        [TestMethod]
        public void EmailConfirmationToken()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this._session));
            userManager.UserTokenProvider = new EmailTokenProvider<ApplicationUser, string>() { BodyFormat = "xxxx {0}", Subject = "Reset password" };
            userManager.Create(new ApplicationUser() { UserName = "test", Email = "aaa@bbb.com", EmailConfirmed = false }, "Welcome");
            var x = userManager.FindByEmail("aaa@bbb.com");
            string token = userManager.GeneratePasswordResetToken(x.Id);
            userManager.ResetPassword(x.Id, token, "Welcome!");
        }

        [TestMethod]
        public void FindByEmailAggregated()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this._session));
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(this._session));
            userManager.Create(new ApplicationUser() { UserName = "test", Email = "aaa@bbb.com", EmailConfirmed = true }, "Welcome");
            var x = userManager.FindByEmail("aaa@bbb.com");
            roleManager.CreateAsync(new IdentityRole("Admin"));
            userManager.AddClaim(x.Id, new Claim("role", "admin"));
            userManager.AddClaim(x.Id, new Claim("role", "user"));
            userManager.AddToRole(x.Id, "Admin");
            userManager.AddLogin(x.Id, new UserLoginInfo("facebook", "1234"));
            this._session.Clear();
            x = userManager.FindByEmail("aaa@bbb.com");
            Assert.IsNotNull(x);
            Assert.AreEqual(2, x.Claims.Count);
            Assert.AreEqual(1, x.Roles.Count);
            Assert.AreEqual(1, x.Logins.Count);
        }
    }
}