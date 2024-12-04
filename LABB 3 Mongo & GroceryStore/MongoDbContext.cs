using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simple.Store
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Customer> _customersCollection;
        private readonly IMongoCollection<Product> _productsCollection;
        private readonly IMongoCollection<CartItem> _cartCollection;

        public IMongoCollection<Customer> CustomersCollection => _customersCollection;
        public IMongoCollection<Product> ProductsCollection => _productsCollection;

        public MongoDbContext(string dbName)
        {
            //string connectionString = "mongodb://localhost:27017/GroceryStore";
            string connectionString = "mongodb+srv://sarabattistella2:DatabaseMongoDB@cluster2.dpzm7.mongodb.net/GroceryStore?retryWrites=true&w=majority";
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(dbName);
            _customersCollection = _database.GetCollection<Customer>("Customers");
            _productsCollection = _database.GetCollection<Product>("products");
            _cartCollection = _database.GetCollection<CartItem>("CartItems");
        }

        // ===========================
        // Customer Methods
        // ===========================

        //Method to get customers
        public List<Customer> GetCustomers() => _customersCollection.Find(_ => true).ToList();

        public Customer GetCustomerByName(string name)
        {
            return _customersCollection.Find(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        // Method to add a customer
        public void AddCustomer(Customer customer)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding customer: {ex.Message}");
            }
        }

        public void UpdateCustomer(Customer customer)
        {
            try
            {
                var update = Builders<Customer>.Update
                    .Set(c => c.Name, customer.Name)
                    .Set(c => c.TotalSpent, customer.TotalSpent);

                var result = _customersCollection.UpdateOne(c => c.Name == customer.Name, update);
                Console.WriteLine(result.MatchedCount > 0 ? "Customer updated successfully." : "Customer not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating customer: {ex.Message}");
            }
        }

        // ===========================
        // Product Methods
        // ===========================

        // Method to list all the products availabe in the db 
        public async Task<List<Product>> GetProducts()
        {
            try
            {
                // Retrieve products from the DB
                var products = await _productsCollection.Find(_ => true).ToListAsync();
                Console.WriteLine($"Retrieved {products.Count} products.");

                // Update product prices in EUR and CHF based on SEK
                foreach (var product in products)
                {
                    // Convert prices from SEK to EUR and CHF using the hardcoded converter
                    product.PriceEUR = CurrencyConverter.ConvertToEUR(product.PriceSEK, "SEK");
                    product.PriceCHF = CurrencyConverter.ConvertToCHF(product.PriceSEK, "SEK");
                }

                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving products: {ex.Message}");
                return new List<Product>();
            }
        }

        // Method to add a product to the db
        public async Task AddProductToDb(Product product)
        {
            try
            {
                // Convert the price from SEK to EUR and CHF before saving the product to the database
                product.PriceEUR = CurrencyConverter.ConvertToEUR(product.PriceSEK, "SEK");
                product.PriceCHF = CurrencyConverter.ConvertToCHF(product.PriceSEK, "SEK");

                // Check if the product already exists in the DB
                var existingProduct = await _productsCollection
                    .Find(p => p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefaultAsync();

                if (existingProduct != null)
                {
                    Console.WriteLine($"Product '{product.Name}' already exists.");
                    return;
                }

                // Insert the product into the database
                await _productsCollection.InsertOneAsync(product);
                Console.WriteLine($"Product '{product.Name}' added successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding product: {ex.Message}");
            }
        }

        public void UpdateProduct(Product product)
        {
            try
            {
                var update = Builders<Product>.Update
                    .Set(p => p.Name, product.Name)
                    .Set(p => p.PriceSEK, product.PriceSEK);

                var result = _productsCollection.UpdateOne(p => p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase), update);
                Console.WriteLine(result.MatchedCount > 0 ? "Product updated successfully." : "Product not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
            }
        }

        // Method to remove a product from the db
        public void RemoveProductFromDb(string productName)
        {
            try
            {
                var result = _productsCollection.DeleteOne(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));
                Console.WriteLine(result.DeletedCount > 0 ? $"Product '{productName}' removed successfully." : $"Product '{productName}' not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing product: {ex.Message}");
            }
        }

        // Fetch a single product by name
        public Product GetProductByName(string productName)
        {
            return _productsCollection.Find(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        // ===========================
        // Cart Methods
        // ===========================

        // Fetch all cart items for a customer from MongoDB
        public async Task<List<CartItem>> GetCartItems(string customerName)
        {
            var filter = Builders<CartItem>.Filter.Eq(c => c.CustomerName, customerName);
            return await _cartCollection.Find(filter).ToListAsync(); // Retrieve the cart items for the specified customer
        }


        // Add a cart item to MongoDB
        public async Task AddCartItem(CartItem cartItem)
        {
            if (!CustomerExists(cartItem.CustomerName))
            {
                Console.WriteLine("Customer not found. Cannot add cart item.");
                return;
            }

            // Add cart item to MongoDB
            await _cartCollection.InsertOneAsync(cartItem);
            Console.WriteLine("Cart item added successfully.");
        }

        // Update cart items in MongoDB
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

        // Clear the cart for a customer in MongoDB
        public async Task ClearCart(string customerName)
        {
            var filter = Builders<CartItem>.Filter.Eq(c => c.CustomerName, customerName);
            var result = await _cartCollection.DeleteManyAsync(filter);
            Console.WriteLine(result.DeletedCount > 0 ? "Cart cleared successfully." : "No cart items found to clear.");
        }

        // ===========================
        // Helper Methods
        // ===========================

        // Helper method to check if the customer exists
        private bool CustomerExists(string name)
        {
            return _customersCollection.Find(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Any();
        }
    }
}
