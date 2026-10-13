using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardAccess
{
    public enum AccessLevel { Guest = 0, Staff = 1, Admin = 2 }

    public record User(int Id, string Name, string Surname, AccessLevel AccessLevel);
}