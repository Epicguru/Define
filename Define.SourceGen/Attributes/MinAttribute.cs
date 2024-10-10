﻿using System;
using SourceGenerator.Helper.CopyCode;

namespace Define.SourceGen.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[Copy]
internal class MinAttribute : Attribute
{
     public MinAttribute(object min, bool enforce = true) { }
}
