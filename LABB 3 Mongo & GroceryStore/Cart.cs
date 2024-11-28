using MongoDB.Bson;
using MongoDB.Driver;


namespace Simple.Store
{
    public class Cart
    {
        public ObjectId Id { get; set; }
        public string CustomerName { get; set; }  // Link to Customer by Name (or use a CustomerId)
        public List<CartItem> Items { get; set; } = new List<CartItem>();  // List of items in the cart

        // Static MongoDB collections for customers and carts
        private static IMongoCollection<Customer> _customersCollection;
        private static IMongoCollection<Cart> _cartCollection;


        public Cart(string customerName)
        {
            CustomerName = customerName;
        }
    }
}