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

        // ===========================
        // Cart Methods
        // ===========================

        // Fetch all cart items for a customer
        //public async Task<List<CartItem>> GetCartItems(string customerName)
        //{
        //    return await _cartCollection
        //        .Find(c => c.CustomerName.Equals(customerName, StringComparison.OrdinalIgnoreCase))
        //        .ToListAsync();
        //}
       
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

        // Clear cart for a customer
        //public bool ClearCart(string customerName)
        //{
        //    var filter = Builders<CartItem>.Filter.Eq(c => c.CustomerName, customerName);
        //    var result = _cartCollection.DeleteMany(filter);
        //    Console.WriteLine(result.DeletedCount > 0
        //        ? "Cart cleared successfully."
        //        : "No cart items found to clear.");
        //    return result.DeletedCount > 0;
        //}
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
