﻿using BusinessObjectLibrary;
using BusinessObjectLibrary.Predefined_constants;
using DataAccessLibrary.Interfaces;
using DTOsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLibrary.Business_Entity
{
    public class UserBusinessEntity
    {
        private IUnitOfWork work;
        public UserBusinessEntity(IUnitOfWork work)
        {
            this.work = work;
        }
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return (await work.Users.GetAllAsync())
                .Where(user => !user.IsDeleted);
        }

        public async Task<IEnumerable<User>> GetMembersAsync()
        {
            var user = (await work.Users.GetAllAsync())
                .Where(user => !user.IsDeleted && user.Role == (int)UserRoles.USER);
            return user;
        }

        public async Task<IEnumerable<User>> GetMembersByUserNameAsync(string keywordUserName)
        {
            var user = (await work.Users.GetAllAsync())
                .Where(
                    user => !user.IsDeleted && 
                    user.Role == (int)UserRoles.USER && 
                    user.Username.ToLower().Contains(keywordUserName.ToLower())
                 );
            return user;
        }

        public async Task<User> GetUserAsync(Guid id)
        {
            return (await work.Users.GetAllAsync())
                .Where(user => !user.IsDeleted && user.Id.Equals(id))
                .FirstOrDefault();
        }
        public async Task<User> AddUser(User user)
        {
            user.Id = Guid.NewGuid();
            user.IsDeleted = false;
            await work.Users.AddAsync(user);
            await work.Save();
            return user;
        }
        public async Task<User> UpdateUser(User user)
        {
            work.Users.Update(user);
            await work.Save();
            return user;
        }
        public async Task RemoveUser(Guid id)
        {
            User user = await work.Users.GetAsync(id);
            user.IsDeleted = true;
            work.Users.Update(user);
            await work.Save();
        }

        public async Task<User> Login(string username, string password)
        {
            return (await work.Users.GetAllAsync())
                .Where((user) => user.Username == username && user.Password == password)
                .FirstOrDefault();
        }

        public async Task<AdminUserByYearDTO> GetAdminUserByYearReport()
        {
            AdminUserByYearDTO adminUserByYear = new AdminUserByYearDTO();
            List<string> listNumberUserByYear = new List<string>();
            IEnumerable<User> users = await work.Users.GetAllAsync();
            int minYear = (from u in users select u.CreatedDate).Min().Year;
            for (int i = minYear; i <= DateTime.Now.Year; i++)
            {
                listNumberUserByYear.Add(i.ToString() + "-" + (await work.Users.GetAllAsync())
                .Where(u => !u.IsDeleted && u.CreatedDate.Year == i).Count().ToString());
            }
            adminUserByYear.UserByYear = listNumberUserByYear;
            return adminUserByYear;
        }
    }
}
