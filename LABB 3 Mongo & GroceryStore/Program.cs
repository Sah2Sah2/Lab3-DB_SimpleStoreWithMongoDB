using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.Store
{
    class Program
    {
        static bool isLoggedIn = false; // Tracks whether the user is logged in
        static Customer loggedInCustomer = null;
        static MongoDbContext dbContext; // MongoDB context

        static async Task Main(string[] args)
        {
            // Define the database name
            string dbName = "GroceryStore";

            // Initialize MongoDB context with the database name
            dbContext = new MongoDbContext(dbName);

            //SeedCustomers();

            bool showMenu = true;
            bool isFirstVisit = true; // Track if it's the first visit

            while (showMenu)
            {
                showMenu = await MainMenu(isFirstVisit);
            }
        }

        //private static void SeedCustomers()
        //{
        //    // Sample customers to be added to MongoDB
        //    var customer1 = new Customer("Sara", "123");
        //    var customer2 = new Customer("Jimmy", "456");
        //    var customer3 = new Customer("Alessia", "789");

        //    // Add customers to the MongoDB collection
        //    dbContext.AddCustomer(customer1);
        //    dbContext.AddCustomer(customer2);
        //    dbContext.AddCustomer(customer3);
        //}

        private static async Task<bool> MainMenu(bool isFirstVisit)
        {
            string username, password;

            if (isFirstVisit && !isLoggedIn)
            {
                Console.WriteLine(@"
 _____________________ 
|                     |  
|    WELCOME TO THE   |  
|     SIMPLE STORE!   |  
|_____________________|");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("\n Here you can find the best products at the best prices in town!");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n ---------------------------------------------------------------");
                isFirstVisit = false; // Update the first visit status
                Console.ReadKey();
            }

            Console.ResetColor();
            if (!isLoggedIn)
            {
                Console.Clear();  // Clear the console before showing login options
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1) Register new customer");
                Console.WriteLine("2) Log in");
                Console.WriteLine("3) Exit");
                Console.Write("\nSelect an option: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        RegisterNewCustomer();
                        isLoggedIn = true; // Automatically logged in after registration
                        return true;

                    case "2":
                        // Prompt for username and password for logging in
                        Console.Write("Enter username: ");
                        username = Console.ReadLine();
                        Console.Write("Enter password: ");
                        password = Console.ReadLine();

                        // Log in and check if successful
                        if (LogIn(username, password))
                        {
                            isLoggedIn = true;
                        }
                        else
                        {
                            Console.WriteLine("Invalid credentials.");
                        }
                        return true;

                    case "3":
                        Console.WriteLine("Exiting the application...");
                        return false; // Close the application

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        return true;
                }
            }
            else
            {
                Console.Clear();  // Clear the console before showing logged-in menu
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1) Go shopping");
                Console.WriteLine("2) Display your account information");
                Console.WriteLine("3) View cart");
                Console.WriteLine("4) Log out");
                Console.Write("\r\nSelect an option: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        await StartShopping();
                        return true;

                    case "2":
                        if (loggedInCustomer != null)
                        {
                            DisplayAccountInformation();
                        }
                        else
                        {
                            Console.WriteLine("No customer is currently logged in.");
                        }
                        Console.WriteLine("\nPress any key to continue");
                        Console.ReadKey();
                        return true;

                    case "3":
                        await ViewCart();
                        return true;

                    case "4":
                        Console.WriteLine("You have been logged out.");
                        isLoggedIn = false;
                        return false;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        return true;
                }
            }
        }


        private static void RegisterNewCustomer()
        {
            // Get customer details and handle registration
            Console.WriteLine("Enter a username: ");
            string name = Console.ReadLine();
            Console.WriteLine("Enter a password: ");
            string password = Console.ReadLine();

            // Check for duplicate username in MongoDB
            if (dbContext.GetCustomerByName(name) != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Username already exists. Please choose a different username.");
                Console.ResetColor();
                return;
            }

            // Create the new customer and add it to the database
            var newCustomer = new Customer(name, password);
            dbContext.AddCustomer(newCustomer);
            loggedInCustomer = newCustomer;
            Console.WriteLine($"Welcome, {loggedInCustomer.Name}! You are now logged in.");
        }

        private static async Task StartShopping()
        {
            var cart = new Dictionary<Product, int>(); // Initialize cart

            // Fetch products asynchronously from MongoDB (only from MongoDB)
            List<Product> productsAvailable = await Product.GetProductsFromMongoDB();

            // Debug log to check if products are available
            Console.WriteLine($"Retrieved {productsAvailable.Count} products from MongoDB.");

            if (productsAvailable.Count == 0)
            {
                Console.WriteLine("No products available in the database.");
                return; // Exit if no products
            }

            bool shopping = true;

            // Show products once
            Console.WriteLine("\nAvailable products:");
            for (int i = 0; i < productsAvailable.Count; i++)
            {
                var product = productsAvailable[i];
                Console.WriteLine($"{i + 1}. {product.Name} - {product.PriceSEK:F2} SEK / {product.PriceEUR:F2} EUR / {product.PriceCHF:F2} CHF");
            }

            // Shopping loop
            while (shopping)
            {
                Console.Write("\nEnter: \n-the number of the product (1\\2\\3\\4\\5) you want to add to the cart\n-or type 'pay' to finish shopping,\n-or 'save' to save cart for later: ");
                string input = Console.ReadLine();

                if (int.TryParse(input, out int productIndex) && productIndex > 0 && productIndex <= productsAvailable.Count)
                {
                    Product selectedProduct = productsAvailable[productIndex - 1];
                    if (cart.ContainsKey(selectedProduct))
                    {
                        cart[selectedProduct]++;
                        Console.WriteLine($"{selectedProduct.Name} quantity increased in the cart.");
                    }
                    else
                    {
                        cart[selectedProduct] = 1;
                        Console.WriteLine($"{selectedProduct.Name} added to the cart.");
                    }
                }
                else if (input.ToLower() == "save")
                {
                    try
                    {
                        SaveCartToMongoDb(cart, loggedInCustomer, dbContext);
                        Console.WriteLine("Cart saved for later. Returning to main menu...");
                        shopping = false; // Exit shopping
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error saving cart: {ex.Message}");
                        Console.ResetColor();
                    }
                }
                else if (input.ToLower() == "pay")
                {
                    try
                    {
                        Console.WriteLine("\nYour cart summary:");
                        foreach (var item in cart)
                        {
                            Console.WriteLine($"{item.Key.Name} x{item.Value} - {item.Key.PriceSEK * item.Value:F2} SEK");
                        }

                        Console.WriteLine("\nProceeding to checkout...");
                        PayForItems(cart, loggedInCustomer);
                        shopping = false; // Exit shopping
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error during payment: {ex.Message}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please try again.");
                }
            }
        }


        public static async Task SaveCartToMongoDb(Dictionary<Product, int> cart, Customer loggedInCustomer, MongoDbContext dbContext)
        {
            if (cart == null || cart.Count == 0)
            {
                Console.WriteLine("Cart is empty, nothing to save.");
                return; // Exit if cart is empty
            }

            try
            {
                // Await the task to get the cart items
                var cartItems = await dbContext.GetCartItems(loggedInCustomer.Name);

                foreach (var item in cart)
                {
                    // Find the existing cart item using LINQ
                    var existingCartItem = cartItems.FirstOrDefault(c => c.ProductName == item.Key.Name);

                    if (existingCartItem != null)
                    {
                        // Update existing cart item quantity
                        existingCartItem.Quantity += item.Value;
                        await dbContext.UpdateCartItem(existingCartItem);  
                        Console.WriteLine($"{item.Key.Name} quantity updated in the cart.");
                    }
                    else
                    {
                        // Add a new cart item
                        var cartItem = new CartItem
                        {
                            CustomerName = loggedInCustomer.Name,
                            ProductName = item.Key.Name,
                            Quantity = item.Value,
                            Price = item.Key.PriceSEK
                        };
                        await dbContext.AddCartItem(cartItem);  // Assuming async add method
                        Console.WriteLine($"{item.Key.Name} added to the cart.");
                    }
                }

                Console.WriteLine("Cart items saved successfully.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error saving cart: {ex.Message}");
                Console.ResetColor();
            }
        }


        public static async Task PayForItems(Dictionary<Product, int> cart, Customer loggedInCustomer)
        {
            decimal total = cart.Sum(item => item.Key.PriceSEK * item.Value);
            Console.WriteLine($"Total price: {total:F2} SEK");

            Console.WriteLine("Proceed with payment? (y/n)");
            string input = Console.ReadLine()?.ToLower();

            if (input == "y")
            {
                // Simulate payment confirmation
                Console.WriteLine("Payment successful!");

                // Update total spent for the logged-in customer
                loggedInCustomer.TotalSpent += total;

                // Display the updated total spent for the customer
                Console.WriteLine($"Total spent for {loggedInCustomer.Name} is now {loggedInCustomer.TotalSpent:F2} SEK");

                // Save the updated customer information to the database
                await UpdateLoggedInCustomerTotalSpent(loggedInCustomer, dbContext);

                // Clear the cart after successful payment
                cart.Clear();
                Console.WriteLine("Cart has been cleared. Thank you for your purchase!");
            }
            else
            {
                Console.WriteLine("Payment canceled. Returning to shopping...");
            }
        }


        public static async Task UpdateLoggedInCustomerTotalSpent(Customer loggedInCustomer, MongoDbContext dbContext)
        {
            // Ensure that the customer collection is initialized
            if (Customer.CustomersCollection == null) // Access via Customer class
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Customer collection is not initialized.");
                Console.ResetColor();
                return;
            }

            // Ensure the loggedInCustomer is not null
            if (loggedInCustomer == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Logged-in customer cannot be null.");
                Console.ResetColor();
                return;
            }

            try
            {
                // Log the current state of the logged-in customer before updating
                Console.WriteLine($"Updating TotalSpent for {loggedInCustomer.Name}. Current TotalSpent: {loggedInCustomer.TotalSpent}");

                // Prepare filter and update definition for MongoDB
                var filter = Builders<Customer>.Filter.Eq(c => c.Name, loggedInCustomer.Name);
                var update = Builders<Customer>.Update.Set(c => c.TotalSpent, loggedInCustomer.TotalSpent);

                // Perform the update operation asynchronously
                var result = await Customer.CustomersCollection.UpdateOneAsync(filter, update);

                // Check if the update was successful
                if (result.ModifiedCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Successfully updated the TotalSpent for {loggedInCustomer.Name}.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"No records were updated for {loggedInCustomer.Name}. Possible reason: Customer not found or already up-to-date.");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error updating TotalSpent for logged-in customer: {ex.Message}");
                Console.ResetColor();
            }
        }


        private static async Task ViewCart()
        {
            // Get the cart items for the logged-in customer using the async method
            var cartItems = await dbContext.GetCartItems(loggedInCustomer.Name);

            if (cartItems.Any())
            {
                Console.WriteLine("Your Cart:");
                decimal totalAmount = 0;

                foreach (var item in cartItems)
                {
                    Console.WriteLine($"{item.ProductName} x {item.Quantity} - {item.Price:C}");
                    totalAmount += item.Price * item.Quantity; // Calculate total amount
                }

                Console.WriteLine($"Total: {totalAmount:C}");

                // Prompt the user to pay or go back
                Console.WriteLine("\nWould you like to pay for the items in your cart?");
                Console.WriteLine("1) Yes, proceed to payment");
                Console.WriteLine("2) No, return to the menu");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        // Simulate payment process (you can add actual logic here later)
                        Console.WriteLine($"Processing payment of {totalAmount:C}...");

                        // Simulate payment success
                        bool paymentSuccessful = ProcessPayment(totalAmount);

                        if (paymentSuccessful)
                        {
                            // After successful payment, update totalSpent and save the updated information
                            loggedInCustomer.TotalSpent += totalAmount;

                            // Display the updated total spent
                            Console.WriteLine($"Total spent for {loggedInCustomer.Name} is now {loggedInCustomer.TotalSpent:C}");

                            // Save the updated customer information to the database
                            await UpdateLoggedInCustomerTotalSpent(loggedInCustomer, dbContext);

                            // Clear the cart or mark items as paid
                            await dbContext.ClearCart(loggedInCustomer.Name); // Clear the cart after payment
                            Console.WriteLine("Payment successful! Your cart has been cleared.");
                        }
                        else
                        {
                            Console.WriteLine("Payment failed. Please try again later.");
                        }
                        break;

                    case "2":
                        Console.WriteLine("Returning to the menu...");
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Your cart is empty.");
            }
        }


        // This method simulates the payment process.
        private static bool ProcessPayment(decimal amount)
        {
            Console.WriteLine($"Payment of {amount:C} processed successfully.");
            return true; // Return true to indicate payment was successful.
        }

        private static void DisplayAccountInformation()
        {
            Console.WriteLine($"Customer: {loggedInCustomer.Name}");
            Console.WriteLine($"Membership Status: {loggedInCustomer.GetMembershipStatus()}");
            Console.WriteLine($"Total Spent: {loggedInCustomer.TotalSpent:C}");
        }

        private static bool LogIn(string username, string password)
        {
            loggedInCustomer = dbContext.GetCustomerByName(username);
            if (loggedInCustomer != null && loggedInCustomer.VerifyPassword(password)) // Use VerifyPassword method
            {
                Console.WriteLine($"Welcome back, {loggedInCustomer.Name}!");
                return true;
            }
            else
            {
                Console.WriteLine("Invalid username or password.");
                return false;
            }
        }
    }
}
