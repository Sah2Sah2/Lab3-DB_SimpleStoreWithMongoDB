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
                Console.WriteLine("3) Inventory Management");
                Console.WriteLine("4) Exit");
                Console.Write("\nSelect an option: ");

                switch (Console.ReadLine())
                {
                    case "1":
                        RegisterNewCustomer();
                        isLoggedIn = true; // Automatically logged in after registration
                        return true;

                    case "2":
                        // Prompt for username
                        Console.Write("Enter username: ");
                        username = Console.ReadLine();

                        // Prompt for password
                        Console.Write("Enter password: ");
                        password = string.Empty;

                        ConsoleKeyInfo key;
                        do
                        {
                            key = Console.ReadKey(intercept: true); 
                            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                            {
                                password = password[0..^1]; 
                                Console.Write("\b \b"); 
                            }
                            else if (!char.IsControl(key.KeyChar))
                            {
                                password += key.KeyChar; 
                                Console.Write("*"); 
                            }
                        } while (key.Key != ConsoleKey.Enter); 
                        Console.WriteLine(); 

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
                        Console.WriteLine("");
                        ShowProductManagementMenu();
                        isLoggedIn = false;
                        return true;

                    case "4":
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
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("\nYou have been logged out.");
                        Console.ResetColor();
                        isLoggedIn = false;
                        return false;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        return true;
                }
            }
        }

        // Method to register a new custumer 
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

        // Method to start the shopping experience
        private static async Task StartShopping()
        {
            var cart = new Dictionary<Product, int>(); // Initialize cart
            List<Product> productsAvailable = await Product.GetProductsFromMongoDB();

            if (!productsAvailable.Any())
            {
                Console.WriteLine("No products available in the database.");
                return;
            }

            Console.WriteLine("\nAvailable products:");
            for (int i = 0; i < productsAvailable.Count; i++)
            {
                var product = productsAvailable[i];
                Console.WriteLine($"{i + 1}. {product.Name} - {product.PriceSEK:F2} SEK / {product.PriceEUR:F2} EUR / {product.PriceCHF:F2} CHF");
            }

            while (true)
            {
                Console.Write("\nEnter: \n-product number to add to the cart\n-'pay' to finish shopping\n-'save' to save the cart: ");
                string input = Console.ReadLine()?.Trim().ToLower();

                if (int.TryParse(input, out int productIndex) && productIndex >= 1 && productIndex <= productsAvailable.Count)
                {
                    Product selectedProduct = productsAvailable[productIndex - 1];
                    AddToCart(cart, selectedProduct);
                }
                else if (input == "save")
                {
                    if (cart.Count == 0)
                    {
                        Console.WriteLine("Cart is empty, nothing to save.");
                    }
                    else
                    {
                        await SaveCartToMongoDb(cart, loggedInCustomer, dbContext);
                        Console.WriteLine("Your cart is saved for later. Returning to main menu...");
                        Console.WriteLine("\nPress any key to contine");
                        Console.ReadKey();
                        return; // Exit shopping
                    }
                }
                else if (input == "pay")
                {
                    if (cart.Count == 0)
                    {
                        Console.WriteLine("Cart is empty, nothing to pay for.");
                    }
                    else
                    {
                        await PayForItems(cart, loggedInCustomer, dbContext);
                        return; // Exit shopping
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please try again.");
                }
            }

        }

        // Method to add items to the cart
        private static void AddToCart(Dictionary<Product, int> cart, Product product)
        {
            if (cart.ContainsKey(product))
            {
                cart[product]++;
                Console.WriteLine($"{product.Name} quantity increased in the cart.");
            }
            else
            {
                cart[product] = 1;
                Console.WriteLine($"{product.Name} added to the cart.");
            }
        }

        // Method to save the cart to MongoDb
        public static async Task SaveCartToMongoDb(Dictionary<Product, int> cart, Customer loggedInCustomer, MongoDbContext dbContext)
        {
            if (cart == null || cart.Count == 0)
            {
                Console.WriteLine("Cart is empty, nothing to save.");
                return; // Exit if cart is empty
            }
            try
            {
                var cartItems = await dbContext.GetCartItems(loggedInCustomer.Name);

                foreach (var item in cart)
                {
                    var existingCartItem = cartItems.FirstOrDefault(c => c.ProductName == item.Key.Name);

                    if (existingCartItem != null)
                    {
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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nCart items saved successfully.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error saving cart: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Mehod to pay for the items 
        public static async Task PayForItems(Dictionary<Product, int> cart, Customer loggedInCustomer, MongoDbContext dbContext)
        {
            decimal total = cart.Sum(item => item.Key.PriceSEK * item.Value);
            Console.WriteLine($"Total price: {total:F2} SEK");

            Console.WriteLine("\nProceed with payment? (y/n)");
            string input = Console.ReadLine()?.ToLower();

            if (input == "y")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Payment successful!");
                Console.ResetColor();

                loggedInCustomer.TotalSpent += total;

                Console.WriteLine($"\nTotal spent for {loggedInCustomer.Name} is now {loggedInCustomer.TotalSpent:F2} SEK");

                await UpdateLoggedInCustomerTotalSpent(loggedInCustomer, dbContext);

                cart.Clear();
                Console.WriteLine("\nCart has been cleared. Thank you for your purchase!");
                Console.Write("\n\nPress any key to continue.");
                Console.ReadKey();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nPayment canceled. Returning to shopping...");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        // Method to update the total spent in MongoDb
        public static async Task UpdateLoggedInCustomerTotalSpent(Customer loggedInCustomer, MongoDbContext dbContext)
        {
            if (loggedInCustomer == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Logged-in customer cannot be null.");
                Console.ResetColor();
                return;
            }
            try
            {
                //Console.WriteLine($"Updating TotalSpent for {loggedInCustomer.Name}. Current TotalSpent: {loggedInCustomer.TotalSpent}"); // Debugging line

                var filter = Builders<Customer>.Filter.Eq(c => c.Name, loggedInCustomer.Name);
                var update = Builders<Customer>.Update.Set(c => c.TotalSpent, loggedInCustomer.TotalSpent);

                var result = await dbContext.CustomersCollection.UpdateOneAsync(filter, update); 

                // Check if the update was successful
                if (result.ModifiedCount > 0)
                {
                    //Console.WriteLine($"Successfully updated the TotalSpent for {loggedInCustomer.Name}."); // Debugging line
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

        // Method to view the content of the cart when items are saved and not purchased
        private static async Task ViewCart()
        {
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

                Console.WriteLine("\nWould you like to pay for the items in your cart?");
                Console.WriteLine("1) Yes, proceed to payment");
                Console.WriteLine("2) No, return to the menu");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        //Console.WriteLine($"Processing payment of {totalAmount:C}..."); // Debugging line

                        bool paymentSuccessful = ProcessPayment(totalAmount);

                        if (paymentSuccessful)
                        {
                            loggedInCustomer.TotalSpent += totalAmount;

                            Console.WriteLine($"Total spent for {loggedInCustomer.Name} is now {loggedInCustomer.TotalSpent:C}");

                            await UpdateLoggedInCustomerTotalSpent(loggedInCustomer, dbContext);

                            await dbContext.ClearCart(loggedInCustomer.Name);
                            //Console.WriteLine("Payment successful!");
                            //Console.WriteLine("Your cart has been cleared.");
                            Console.ReadKey();
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nYour cart is empty.");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\nPress any key to continue");
                Console.ReadKey();
            }
        }

        // This method simulates the payment process
        private static bool ProcessPayment(decimal amount)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nPayment of {amount:C} processed successfully.");
            Console.ResetColor();
            return true;
        }

        // Method to display all the info of the logged in customer
        private static void DisplayAccountInformation()
        {
            Console.WriteLine($"\nCustomer: {loggedInCustomer.Name}");
            Console.WriteLine($"Membership Status: {loggedInCustomer.GetMembershipStatus()}");
            Console.WriteLine($"Total Spent: {loggedInCustomer.TotalSpent:C}");
        }

        // Method to log in 
        private static bool LogIn(string username, string password)
        {
            loggedInCustomer = dbContext.GetCustomerByName(username);
            if (loggedInCustomer != null && loggedInCustomer.VerifyPassword(password))
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

        public static async void ShowProductManagementMenu()
        {
            Console.WriteLine("Product Management:");
            Console.WriteLine("1. Add Product");
            Console.WriteLine("2. Remove Product");
            Console.WriteLine("3. Exit");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    // Add Product
                    Console.WriteLine("Enter product name:");
                    string productName = Console.ReadLine();

                    Console.WriteLine("Enter product price in SEK:");
                    decimal productPriceSEK = decimal.Parse(Console.ReadLine());

                    // Create a new product instance
                    var newProduct = new Product
                    {
                        Name = productName,
                        PriceSEK = productPriceSEK
                    };

                    // Add the product to the database
                   await dbContext.AddProductToDb(newProduct);

                    var addedProduct = dbContext.GetProductByName(productName); 

                    if (addedProduct != null)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"\nProduct '{addedProduct.Name}' was added successfully with a price of {addedProduct.PriceSEK} SEK.");
                      
                    }
                    else
                    {
                        Console.WriteLine("There was an issue adding the product to the database.");
                    }

                    break;

                case "2":
                    // Remove Product
                    Console.WriteLine("Enter the name of the product to remove:");
                    string removeProductName = Console.ReadLine();

                    // Call the RemoveProduct method
                    dbContext.RemoveProductFromDb(removeProductName);

                    // Refresh the product list after removing
                    var refreshedProducts = dbContext.GetProducts().Result; 
                    break;

                case "3":
                    Console.WriteLine("Exiting...");
                    break;

                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
    }
}
