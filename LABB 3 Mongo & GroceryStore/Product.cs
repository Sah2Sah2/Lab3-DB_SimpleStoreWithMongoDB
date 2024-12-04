using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Simple.Store
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("PriceSEK")]
        public decimal PriceSEK { get; set; }

        [BsonElement("PriceEUR")]
        public decimal PriceEUR { get; set; }

        [BsonElement("PriceCHF")]
        public decimal PriceCHF { get; set; }

        [BsonElement("Quantity")]
        public int Quantity { get; set; }

        // MongoDB setup
        private static readonly IMongoClient MongoClient = new MongoClient("mongodb+srv://sarabattistella2:DatabaseMongoDB@cluster2.dpzm7.mongodb.net/GroceryStore?retryWrites=true&w=majority");
        private static readonly IMongoDatabase Database = MongoClient.GetDatabase("GroceryStore");
        private static readonly IMongoCollection<Product> ProductCollection = Database.GetCollection<Product>("products");

        // Static dictionary for cart storage (thread-safe access)
        private static readonly Dictionary<Product, int> Cart = new Dictionary<Product, int>();

        // Override Equals and GetHashCode for comparison
        public override bool Equals(object obj)
        {
            if (obj is Product other)
            {
                return Name == other.Name && PriceSEK == other.PriceSEK;
            }
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(Name, PriceSEK);

        // Add a product to the cart
        public static async Task AddProductToCart(MongoDbContext dbContext, string customerName, Product product, int quantity)
        {
            if (quantity <= 0)
            {
                Console.WriteLine($"Invalid quantity for {product.Name}. Quantity must be greater than 0.");
                return;
            }

            // Fetch the cart item for this product
            var cartItem = await dbContext.GetCartItems(customerName);
            var existingItem = cartItem.FirstOrDefault(item => item.ProductName == product.Name);

            if (existingItem != null)
            {
                // Update the existing cart item
                existingItem.Quantity += quantity;
                await dbContext.UpdateCartItem(existingItem);
            }
            else
            {
                // Add new cart item
                var newCartItem = new CartItem
                {
                    CustomerName = customerName,
                    ProductName = product.Name,
                    Quantity = quantity,
                    //PriceSEK = product.PriceSEK
                };

                await dbContext.AddCartItem(newCartItem);
            }

            Console.WriteLine($"Updated {product.Name} in the cart.");
        }

        // Clear the cart
        public static void ClearCart()
        {
            lock (Cart)
            {
                Cart.Clear();
            }
        }

        // Get the total amount in all currencies
        public static (decimal totalSEK, decimal totalEUR, decimal totalCHF) GetCartAmount()
        {
            decimal totalSEK = 0, totalEUR = 0, totalCHF = 0;

            lock (Cart)
            {
                foreach (var item in Cart)
                {
                    totalSEK += item.Key.PriceSEK * item.Value;
                    totalEUR += item.Key.PriceEUR * item.Value;
                    totalCHF += item.Key.PriceCHF * item.Value;
                }
            }

            return (totalSEK, totalEUR, totalCHF);
        }

        // Save the cart to a file
        public static void SaveCart(string cartFilePath)
        {
            try
            {
                lock (Cart)
                {
                    if (Cart.Count == 0)
                    {
                        Console.WriteLine("Cart is empty. Nothing to save.");
                        return;
                    }

                    using StreamWriter writer = new StreamWriter(cartFilePath, false);
                    foreach (var item in Cart)
                    {
                        writer.WriteLine($"{item.Key.Name},{item.Key.PriceSEK:F2},{item.Key.PriceEUR:F2},{item.Key.PriceCHF:F2},{item.Value}");
                    }
                }

                Console.WriteLine("Cart saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving cart: {ex.Message}");
            }
        }

        // Load the cart from a file
        public static void LoadCart(string cartFilePath)
        {
            try
            {
                lock (Cart)
                {
                    Cart.Clear();

                    using StreamReader reader = new StreamReader(cartFilePath);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(',');
                        if (parts.Length == 5 &&
                            decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal priceSEK) &&
                            int.TryParse(parts[4], out int quantity))
                        {
                            var product = new Product { Name = parts[0], PriceSEK = priceSEK };
                            Cart[product] = quantity;
                        }
                    }
                }

                Console.WriteLine("Cart loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cart: {ex.Message}");
            }
        }

        // View products in the cart
        public static void ViewProductsInCart()
        {
            lock (Cart)
            {
                if (Cart.Count == 0)
                {
                    Console.WriteLine("Your cart is empty.");
                    return;
                }

                Console.WriteLine("\nCart contents:");
                foreach (var item in Cart)
                {
                    Console.WriteLine($"- {item.Key.Name}: {item.Value} pcs at {item.Key.PriceSEK:F2} SEK each");
                }

                var totals = GetCartAmount();
                Console.WriteLine($"Total: {totals.totalSEK:F2} SEK, {totals.totalEUR:F2} EUR, {totals.totalCHF:F2} CHF");
            }
        }


        // Process payment for cart
        public static void PayForItems()
        {
            var totals = GetCartAmount();
            Console.WriteLine($"Total to pay: {totals.totalSEK:F2} SEK");
            Console.WriteLine("Payment successful! Thank you for shopping.");

            ClearCart();
        }

        // MongoDB methods
        public static async Task<List<Product>> GetProductsFromMongoDB()
        {
            try
            {
                var products = await ProductCollection.Find(new BsonDocument()).ToListAsync();
                Console.WriteLine($"Fetched {products.Count} products from MongoDB.");
                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return new List<Product>();
            }
        }

        // Display products only from MongoDB
        public static async Task ViewAvailableProducts()
        {
            var products = await GetProductsFromMongoDB(); // Fetch products from MongoDB

            if (products.Count == 0)
            {
                Console.WriteLine("No products available.");
                return;
            }

            Console.WriteLine("\nAvailable Products:");
            foreach (var product in products)
            {
                Console.WriteLine($"- {product.Name}: {product.PriceSEK:F2} SEK, Quantity: {product.Quantity}");
            }
        }

        // Add, update, and delete products in MongoDB
        public static void AddProductToMongoDB(Product product) => ProductCollection.InsertOne(product);

        public static void UpdateProductInMongoDB(Product product)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.Name, product.Name);
            ProductCollection.ReplaceOne(filter, product);
        }

        public static void DeleteProductFromMongoDB(string productName)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.Name, productName);
            ProductCollection.DeleteOne(filter);
        }
    }
}
