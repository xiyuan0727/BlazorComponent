﻿using Microsoft.AspNetCore.Components;

namespace BlazorComponent
{
    public partial class BStepperItems
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
