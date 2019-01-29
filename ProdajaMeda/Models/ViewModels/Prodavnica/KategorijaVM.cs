using ProdajaMeda.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProdajaMeda.Models.ViewModels.Prodavnica
{
    public class KategorijaVM
    {
        public KategorijaVM()
        {

        }
        public KategorijaVM(KategorijaDTO row)
        {
            Id = row.Id;
            Name = row.Name;
            Slug = row.Slug;
            Sorting = row.Sorting;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public int Sorting { get; set; }
    }
}