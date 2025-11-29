using System;
using System.Collections.Generic;
using System.Linq;
using Rent.Data;
using Rent.Models;

namespace Rent
{
    public class Seed
    {
        private readonly DataContext dataContext;

        public Seed(DataContext context)
        {
            this.dataContext = context;
        }

        public void SeedDataContext()
        {
            if (!dataContext.Users.Any())
            {
                var rentalInfo = new RentalInfo()
                {
                    OpenHour = new TimeSpan(8, 0, 0),
                    CloseHour = new TimeSpan(18, 0, 0),
                    Address = "ul. Centralna1",
                    PhoneNumber = "123456789",
                    OpenDays = "Mon-Fri",
                    Email = "info@rental.com",
                    Users = new List<User>(),
                    Workers = new List<Worker>(),
                    Equipment = new List<Equipment>(),
                    Orders = new List<Order>()
                };

                var user1 = new User()
                {
                    First_name = "Paweł",
                    Last_name = "Kowalski",
                    Login = "pawel",
                    PasswordHash = "password-hash",
                    Email = "pawel@example.com",
                    Phone_number = "111222333",
                   
                    Role = "user",
                    RentalInfo = rentalInfo,
                    Orders = new List<Order>()
                };

                var user2 = new User()
                {
                    First_name = "Anna",
                    Last_name = "Nowak",
                    Login = "anna",
                    PasswordHash = "password-hash",
                    Email = "anna@example.com",
                    Phone_number = "444555666",
            
                    Role = "user",
                    RentalInfo = rentalInfo,
                    Orders = new List<Order>()
                };

                rentalInfo.Users.Add(user1);
                rentalInfo.Users.Add(user2);

                var worker1 = new Worker()
                {
                    First_name = "Jan",
                    Last_name = "Kowal",
                    Email = "jan@example.com",
                    Phone_number = "777888999",
                    Address = "ul. Działkowa3",
                    Role = "administrator",
                    WorkStart = new TimeSpan(8, 0, 0),
                    WorkEnd = new TimeSpan(16, 0, 0),
                    Working_Days = "Mon-Fri",
                    Job_Title = "Manager",
                    RentalInfo = rentalInfo
                };

                var worker2 = new Worker()
                {
                    First_name = "Ewa",
                    Last_name = "Zielińska",
                    Email = "ewa@example.com",
                    Phone_number = "222333444",
                    Address = "ul. Kwiatowa5",
                    Role = "worker",
                    WorkStart = new TimeSpan(10, 0, 0),
                    WorkEnd = new TimeSpan(18, 0, 0),
                    Working_Days = "Mon-Fri",
                    Job_Title = "Cashier",
                    RentalInfo = rentalInfo
                };

                rentalInfo.Workers.Add(worker1);
                rentalInfo.Workers.Add(worker2);

                var eq1 = new Equipment()
                {
                    Size = "M",
                    Is_In_Werehouse = true,
                    Price = 100m,
                };

                var eq2 = new Equipment()
                {
                    Size = "L",
                    Is_In_Werehouse = true,
                    Price = 150m,
                };

                rentalInfo.Equipment.Add(eq1);
                rentalInfo.Equipment.Add(eq2);

                var order1 = new Order()
                {
                    Rented_Items = "Equipment1",
                    OrderDate = DateTime.Now,
                    Price = 100m,
                    Date_Of_submission = DateOnly.FromDateTime(DateTime.Now),
                    Was_It_Returned = false,
                };

                rentalInfo.Orders.Add(order1);
                user1.Orders.Add(order1);

                dataContext.RentalInfo.Add(rentalInfo);
                dataContext.SaveChanges();
            }
        }
    }
}
