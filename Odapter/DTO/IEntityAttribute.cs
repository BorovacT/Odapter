﻿//------------------------------------------------------------------------------
//    Odapter - a C# code generator for Oracle packages
//    Copyright(C) 2018 Clay Lipscomb
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.If not, see<http://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using System;

namespace Odapter {
    /// <summary>
    /// Interface of an entity attribute. Should be implemented mapping to underlying sys view column if naming is different.
    /// </summary>
    internal interface IEntityAttribute {
        string EntityName { get; set; }
        string AttrName { get; set; }
        string AttrType { get; set; }
        string AttrTypeOwner { get; set; }
        string AttrTypeMod { get; set; }
        int? Length { get; set; }
        int? Precision { get; set; }
        int? Scale { get; set; }
        int Position { get; set; }
        bool Nullable { get; }
        string CSharpType { get; set; } // optionally set during load of data (e.g., package record type fields)
        String ContainerClassName { get; set; } // Container class if C# type is nested class
    }
}
