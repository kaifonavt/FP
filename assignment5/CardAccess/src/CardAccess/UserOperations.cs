using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardAccess
{
    public static class UserOperations
    {
        public static IEnumerable<User> AddUser(IEnumerable<User> currentUsers, User newUser) =>
            currentUsers.Append(newUser);

        public static IEnumerable<User> RemoveUser(IEnumerable<User> currentUsers, int userId) =>
            currentUsers.Where(user => user.Id != userId);

        public static IEnumerable<User> AlterUser(IEnumerable<User> currentUsers, User updatedUser) =>
            currentUsers.Select(user => user.Id == updatedUser.Id ? updatedUser : user);
    }
}te