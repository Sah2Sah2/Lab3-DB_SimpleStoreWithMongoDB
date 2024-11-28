using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Simple.Store
{
    public class Customer
    {
        public ObjectId Id { get; set; } // MongoDB _id field

        [BsonElement("name")] // Maps the field name in MongoDB
        public string Name { get; private set; }

        [BsonElement("password")]
        private string Password { get; set; }

        public Dictionary<Product, int> _shoppingCart { get; set; }

        [BsonElement("TotalSpent")]
        public decimal TotalSpent { get; set; }

        // MongoDB collections for customers and carts
        private static IMongoCollection<Customer> _customersCollection;
        private static IMongoCollection<Cart> _cartCollection; // Collection for carts


        // Static constructor to initialize MongoDB client and collections
        static Customer()
        {
            try
            {
                var client = new MongoClient("mongodb://localhost:27017/GroceryStore");
                var database = client.GetDatabase("GroceryStore"); // MongoDB database name
                _customersCollection = database.GetCollection<Customer>("Customer"); // Customer collection
                _cartCollection = database.GetCollection<Cart>("Cart"); // Cart collection
                Console.WriteLine("MongoDB connection established successfully.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error connecting to MongoDB: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Public static property to expose _customersCollection
        public static IMongoCollection<Customer> CustomersCollection
        {
            get
            {
                return _customersCollection;
            }
        }

        // Constructor to initialize customer details
        public Customer(string name, string password)
        {
            Name = name;
            Password = password;
            _shoppingCart = new Dictionary<Product, int>();
        }
       
        // Verify password method 
        public bool VerifyPassword(string password)
        {
            if (Password == null)
            {
                return false; 
            }

            // Compare passwords after trimming whitespaces
            return Password.Trim() == password.Trim();
        }

        public override string ToString()
        {
            string cartItems = _shoppingCart.Count > 0
                ? string.Join(", ", _shoppingCart.Select(kvp => $"{kvp.Value} pcs {kvp.Key.Name}"))
                : "No items in cart";
            return $"Name: {Name}, Shopping Cart: {cartItems}, Total Spent: {TotalSpent} kr, Membership Status: {GetMembershipStatus()}";
        }

        // Method to get the password conditionally
        public string GetPassword(bool showPassword)
        {
            return showPassword ? Password : new string('*', Password.Length); // Show or hide password
        }

        // LogIn method 
        public static bool LogIn(out string? username, out string? password, string cartfilePath)
        {
            username = string.Empty;
            password = string.Empty;

            // Debugging output to confirm customer loading
            Console.WriteLine($"Debug: Customers count before login: {_customersCollection.CountDocuments(FilterDefinition<Customer>.Empty)}");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("Enter your name: ");
            Console.ResetColor();
            string? name = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(name))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid choice. Please, enter your name.");
                Console.ResetColor();
                Console.Write("Enter your name: ");
                name = Console.ReadLine();
            }

            // Check if customer exists in MongoDB
            var filter = Builders<Customer>.Filter.Eq(c => c.Name, name);
            Customer? customer = _customersCollection.Find(filter).FirstOrDefault();

            if (customer != null)
            {
                bool passwordCorrect = false;

                while (!passwordCorrect)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("Enter password: ");
                    Console.ResetColor();
                    string enteredPassword = Console.ReadLine();

                    while (string.IsNullOrWhiteSpace(enteredPassword))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid choice. Please, enter your password.");
                        Console.ResetColor();
                        Console.Write("Enter password: ");
                        enteredPassword = Console.ReadLine();
                    }

                    // Check if the entered password matches the stored password
                    if (customer.VerifyPassword(enteredPassword))
                    {
                        username = customer.Name;
                        password = customer.GetPassword(true);
                        Console.WriteLine($"Welcome back, {customer.Name}!");

                        // Check if a cart exists for the customer
                        var cartFilter = Builders<Cart>.Filter.Eq(c => c.CustomerName, customer.Name);
                        var existingCart = _cartCollection.Find(cartFilter).FirstOrDefault();

                        if (existingCart != null)
                        {
                            Console.WriteLine("Loaded existing cart.");
                        }
                        else
                        {
                            Console.WriteLine("No cart found. Creating a new cart.");
                            // Create a new cart if none exists
                            Cart newCart = new Cart(customer.Name);
                            _cartCollection.InsertOne(newCart);  // Store the new cart in MongoDB
                        }

                        return true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Password incorrect. Try again.");
                        Console.ResetColor();

                        Console.Write("Do you want to try again? (Yes/No): ");
                        string retry = Console.ReadLine()?.ToLower();

                        if (retry != "yes")
                        {
                            Console.WriteLine("Exiting login...");
                            return false;
                        }
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nLogin failed. Customer \"{name}\" does not exist.");
                Console.ResetColor();

                if (AskToRegister())
                {
                    HandleCustomers(out string? newUsername, out string? newPassword);

                    // Check if the new customer name already exists in the database
                    var existingCustomer = _customersCollection.Find(c => c.Name == newUsername).FirstOrDefault();

                    if (existingCustomer != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"A customer with the name \"{newUsername}\" already exists. Please choose a different name.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Customer newCustomer = new Customer(newUsername, newPassword);
                        try
                        {
                            _customersCollection.InsertOne(newCustomer); // Add the new customer to MongoDB
                            username = newUsername;
                            password = newPassword;

                            // Create a new cart for the newly registered customer
                            Cart newCart = new Cart(newUsername);
                            _cartCollection.InsertOne(newCart);  // Add the new cart to MongoDB

                            Console.WriteLine($"Customer data saved successfully for {username}.");
                            Console.WriteLine($"Welcome to the Simple Store, {username}!");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error saving customer: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }
            }
            return false;
        }

        public static bool AskToRegister()
        {
            while (true)
            {
                Console.WriteLine("\nWould you like to register a new customer?");
                Console.WriteLine("1) Yes, I would like to join the Simple store!");
                Console.WriteLine("2) No. I want to log out.");
                Console.Write("\nSelect an option: ");

                string? input = Console.ReadLine();

                if (input == "1")
                {
                    HandleCustomers(out string? username, out string? password); 
                    return true; 
                }
                else if (input == "2")
                {
                    Console.WriteLine("Logging out...");
                    return false; // Log out
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option. Please enter 1 to register or 2 to log out.");
                    Console.ResetColor();
                }
            }
        }

        // Method for discount/membership
        public virtual string GetMembershipStatus()
        {
            if (TotalSpent >= 500)
                return "Gold Member!";
            else if (TotalSpent >= 200)
                return "Silver Member!";
            else if (TotalSpent >= 150)
                return "Bronze Member!";
            else
                return "Regular Member!";
        }

        public static bool HandleCustomers(out string? username, out string? password)
        {
            username = null;
            password = null;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("Enter your name: ");
            Console.ResetColor();
            username = Console.ReadLine();

            // Validate the username input
            while (string.IsNullOrWhiteSpace(username))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please, enter your name.");
                Console.ResetColor();
                Console.Write("Enter your name: ");
                username = Console.ReadLine();
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("Enter your new password: ");
            Console.ResetColor();
            password = Console.ReadLine();

            // Validate the password input
            while (string.IsNullOrWhiteSpace(password))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid input. Please, enter your password.");
                Console.ResetColor();
                Console.Write("Enter your password: ");
                password = Console.ReadLine();
            }
            Console.WriteLine("Customer details received successfully.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();

            return true;
        }
    }
}
