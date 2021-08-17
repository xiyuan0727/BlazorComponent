﻿using Microsoft.AspNetCore.Components;

namespace BlazorComponent
{
    public partial class BResponsiveContent<TResponsive> : ComponentAbstractBase<TResponsive>
        where TResponsive : IResponsive
    {
        public RenderFragment ComponentChildContent => Component.ChildContent;
    }
}