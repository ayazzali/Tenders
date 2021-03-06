﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TenderPlanAPI.Models
{
    public class Customer:ModelBase
    {
        /// <summary>
        /// Наименование поставщика
        /// </summary>
        [Display(Name = "Наименование заказчика")]
        public string Name { get; set; }
        /// <summary>
        /// Номер поставщика
        /// </summary>
        [Display(Name = "Номер поставщика")]
        public string RegNum { get; set; }

        /// <summary>
        /// ИНН заказчика
        /// </summary>
        [Display(Name = "ИНН заказчика")]
        public string INN { get; set; }

        // <summary>
        // Адрес
        // </summary>
        [Display(Name = "Адрес")]
        public string Address { get; set; }
    }
}
