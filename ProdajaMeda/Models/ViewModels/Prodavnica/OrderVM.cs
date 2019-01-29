using ProdajaMeda.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProdajaMeda.Models.ViewModels.Prodavnica
{
    public class OrderVM
    {
        public OrderVM()
        {
        }

        public OrderVM(OrderDTO row)
        {
            OrderId = row.OrderId;
            UserId = row.UserId;
            CreatedAt = row.CreatedAt;
        }

        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}