﻿using System.Collections.Generic;

namespace Model.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public StoreType StoreType { get; set; }
        public string Url { get; set; }
        public string Ingredients { get; set; }
        public ICollection<Ingredient> MatchedIngredients { get; set; } 
    }
}
