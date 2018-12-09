﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Models
{
    public class Product
    {
        public Product() {
            MatchedIngredients = new JoinCollectionFacade<Ingredient, ProductIngredient>(
                ProductIngredients,
                _ => _.Ingredient,
                ingredient => new ProductIngredient { Product = this, Ingredient = ingredient }
            );
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public StoreType StoreType { get; set; }
        public string Url { get; set; }
        public string Ingredients { get; set; }
        public ICollection<ProductIngredient> ProductIngredients { get; } = new List<ProductIngredient>();
        [NotMapped]
        public ICollection<Ingredient> MatchedIngredients { get; }
        public bool IsVegan { get; set; }
    }

    public class ProductIngredient
    {
        public int ProductId { get; set; }
        public int IngredientId { get; set; }
        public virtual Product Product { get; set; }
        public virtual Ingredient Ingredient { get; set; }
    }
}
