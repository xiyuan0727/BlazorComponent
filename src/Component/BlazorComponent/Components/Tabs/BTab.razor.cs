﻿using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorComponent
{
    public partial class BTab : BGroupItem<ItemGroupBase>
    {
        public BTab() : base(GroupType.SlideGroup)
        {
        }

        [CascadingParameter]
        public BTabs Tabs { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        public async Task HandleClick(MouseEventArgs args)
        {
            await ToggleItem();
            
            await OnClick.InvokeAsync(args);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Tabs.CallSlider();
            }
        }
    }
}