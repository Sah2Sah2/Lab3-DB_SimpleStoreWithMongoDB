using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Driver;


namespace Simple.Store
{
    public class MemberCustomer : Customer //Inheritance
    {
        public MemberCustomer(string name, string password) : base(name, password) { }

        public decimal CalculateTotalSpent()
        {
            return TotalSpent;
        }

        public override string GetMembershipStatus() // Override 
        {
            return base.GetMembershipStatus();
        }
    }
}

