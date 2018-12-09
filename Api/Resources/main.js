﻿import "datatables.net";
import "materialize-css";
require("expose-loader?$!jquery");

require("./main.scss");

require("expose-loader?Ingredient!../Views/Ingredient/Ingredient");
require("expose-loader?Layout!../Views/Shared/Layout");
require("expose-loader?Product!../Views/Product/Product");
require("expose-loader?Utils!./Utils");

$(function() {
    Layout.initLayout();
});