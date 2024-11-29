using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Simple.Store
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Customer> _customersCollection;
        private readonly IMongoCollection<Product> _productsCollection;
        private readonly IMongoCollection<CartItem> _cartCollection;
        
        public IMongoCollection<Customer> CustomersCollection => _customersCollection; // public
        public IMongoCollection<Product> ProductsCollection => _productsCollection;

        // Constructor to initialize MongoDB context
        public MongoDbContext(string dbName)
        {
            string connectionString = "mongodb://localhost:27017/GroceryStore"; // MongoDB connection string
            var client = new MongoClient(connectionString); // Initialize client
            _database = client.GetDatabase(dbName); // Get database reference
            _customersCollection = _database.GetCollection<Customer>("Customers");
            _productsCollection = _database.GetCollection<Product>("Products");
            _cartCollection = _database.GetCollection<CartItem>("CartItems");
       
        }

        // ===========================
        // Customer Methods
        // ===========================

        // Fetch all customers
        public List<Customer> GetCustomers() => _customersCollection.Find(_ => true).ToList();

        // Fetch all products
        public IMongoCollection<Product> Products => _database.GetCollection<Product>("products");

        // Fetch a customer by name
        public Customer GetCustomerByName(string name)
        {
            return _customersCollection.Find(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        // Add a new customer
        public void AddCustomer(Customer customer)
        {
            var existingCustomer = GetCustomerByName(customer.Name);
            if (existingCustomer != null)
            {
                Console.WriteLine("Customer already exists.");
                return;
            }

            _customersCollection.InsertOne(customer);
            Console.WriteLine("Customer added successfully.");
        }

        // Update customer details
        public void UpdateCustomer(Customer customer)
        {
            // Build an update definition for multiple fields
            var update = Builders<Customer>.Update
                .Set(c => c.Name, customer.Name)
                .Set(c => c.TotalSpent, customer.TotalSpent);
                //.Set(c => c.MembershipStatus, customer.MembershipStatus);

            // Update the customer document where the name matches
            var result = _customersCollection.UpdateOne(
                c => c.Name == customer.Name,
                update
            );

            // Provide feedback based on the operation result
            Console.WriteLine(result.MatchedCount > 0
                ? "Customer updated successfully."
                : "Customer not found.");
        }

        // ===========================
        // Product Methods
        // ===========================

        // Fetch all products from MongoDB
        public async Task<List<Product>> GetProducts()
        {
            try
            {
                var productsCollection = _database.GetCollection<Product>("products");
                var products = await productsCollection.Find(_ => true).ToListAsync();

                Console.WriteLine($"Retrieved {products.Count} products from MongoDB.");

                // Optional: Print product details to verify the fetched data
                foreach (var product in products)
                {
                    Console.WriteLine($"Product: {product.Name}, PriceSEK: {product.PriceSEK}, PriceEUR: {product.PriceEUR}, PriceCHF: {product.PriceCHF}");
                }

                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving products: {ex.Message}");
                return new List<Product>(); // Return an empty list in case of error
            }
        }

        // Add a product to the database
        public void AddProduct(Product product)
        {
            _productsCollection.InsertOne(product);
            Console.WriteLine("Product added successfully.");
        }

        // Fetch a single product by name
        public Product GetProductByName(string productName)
        {
            return _productsCollection.Find(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public void AddProductToDb(Product product)
        {
            // Check if the product already exists in the database based on its name
            var existingProduct = _productsCollection.Find(p => p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (existingProduct != null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Product '{product.Name}' already exists.");
                Console.ResetColor();
                return; // Exit early if the product already exists
            }

            // If the product does not exist, insert the new product into the databaseF
            _productsCollection.InsertOne(product);
            var insertedProduct = _productsCollection
            .Find(p => p.Name == product.Name)
            .FirstOrDefault();
            if (insertedProduct != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Product '{insertedProduct.Name}' was added to the database.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Product insertion failed.");
                Console.ResetColor();
            }

            // Provide feedback to the user
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Product '{product.Name}' added successfully.");
            Console.ResetColor();
        }

        public void RemoveProductFromDb(string productName)
        {
            var result = _productsCollection.DeleteOne(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));

            if (result.DeletedCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Product '{productName}' removed successfully.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Product '{productName}' not found.");
                Console.ResetColor();
            }
        }

        public void UpdateProduct(Product product)
        {
            try
            {
                // Build an update definition for the fields you want to update
                var update = Builders<Product>.Update
                    .Set(p => p.Name, product.Name)
                    .Set(p => p.PriceSEK, product.PriceSEK);
                    //.Set(p => p.PriceEUR, product.PriceEUR)
                    //.Set(p => p.PriceCHF, product.PriceCHF);

                // Update the product document where the name matches (case-insensitive)
                var result = _productsCollection.UpdateOne(
                    p => p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase),
                    update
                );

                // Provide feedback based on the operation result
                Console.WriteLine(result.MatchedCount > 0
                    ? "Product updated successfully."
                    : "Product not found.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error updating product: {ex.Message}");
                Console.ResetColor();
            }
        }




        // ===========================
        // Cart Methods
        // ===========================

        public async Task<List<CartItem>> GetCartItems(string customerName)
        {
            var filter = Builders<CartItem>.Filter.Eq(c => c.CustomerName, customerName);
            return await _cartCollection.Find(filter).ToListAsync(); // Retrieve the cart items for the specified customer
        }

        // Add a cart item asynchronously
        public async Task AddCartItem(CartItem cartItem)
        {
            if (!CustomerExists(cartItem.CustomerName))
            {
                Console.WriteLine("Customer not found. Cannot add cart item.");
                return;
            }

            await _cartCollection.InsertOneAsync(cartItem); 
            Console.WriteLine("Cart item added successfully.");
        }
        
        // Update cart items asynchronously
        public async Task UpdateCartItem(CartItem cartItem)
        {
            var filter = Builders<CartItem>.Filter.And(
                Builders<CartItem>.Filter.Eq(c => c.CustomerName, cartItem.CustomerName),
                Builders<CartItem>.Filter.Eq(c => c.ProductName, cartItem.ProductName)
            );

            var update = Builders<CartItem>.Update
                .Set(c => c.Quantity, cartItem.Quantity);

            var result = await _cartCollection.UpdateOneAsync(filter, update); 
            Console.WriteLine(result.MatchedCount > 0
                ? "Cart item updated successfully."
                : "Cart item not found.");
        }

        // Clear the cart for a customer
        public async Task ClearCart(string customerName)
        {
            var filter = Builders<CartItem>.Filter.Eq(c => c.CustomerName, customerName);
            var result = await _cartCollection.DeleteManyAsync(filter);
            Console.WriteLine(result.DeletedCount > 0 ? "Cart cleared successfully." : "No cart items found to clear.");
        }

        // ===========================
        // Helper Methods
        // ===========================

        // Check if a customer exists
        private bool CustomerExists(string name)
        {
            return _customersCollection.Find(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Any();
        }
    }
}
