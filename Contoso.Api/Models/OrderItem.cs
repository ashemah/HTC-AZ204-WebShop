using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Contoso.Api.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }
    }
}