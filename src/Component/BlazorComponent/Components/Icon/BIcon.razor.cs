﻿using BlazorComponent.Helpers;
using BlazorComponent.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorComponent
{
    public partial class BIcon : IThemeable, ITransitionIf
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool If { get; set; } = true;

        #region IIcon

        [Parameter]
        public bool Dense { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool Left { get; set; }

        [Parameter]
        public bool Right { get; set; }

        [Parameter]
        public StringNumber Size { get; set; }

        public string Icon { get; set; }

        [Parameter]
        public string Tag { get; set; } = "i";

        public string NewChildren { get; set; }

        public Dictionary<string, object> SvgAttrs { get; set; }

        #endregion

        [Parameter]
        public bool Dark { get; set; }

        [Parameter]
        public bool Light { get; set; }

        [CascadingParameter(Name = "IsDark")]
        public bool CascadingIsDark { get; set; }

        public bool IsDark
        {
            get
            {
                if (Dark)
                {
                    return true;
                }

                if (Light)
                {
                    return false;
                }

                return CascadingIsDark;
            }
        }

        [Parameter]
        public string Color { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter]
        public bool OnClickPreventDefault { get; set; }

        [Parameter]
        public bool OnClickStopPropagation { get; set; }

        [Inject]
        public Document Document { get; set; }

        private bool _clickEventRegistered;

        protected override Task OnParametersSetAsync()
        {
            InitIcon();
            return base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!_clickEventRegistered)
            {
                await TryRegisterClickEvent();
            }
        }

        protected override async Task OnElementReferenceChangedAsync()
        {
            await TryRegisterClickEvent();
        }

        public virtual async Task HandleOnClick(MouseEventArgs args)
        {
            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(args);
            }
        }

        private async Task TryRegisterClickEvent()
        {
            if (Ref.Context is not null && OnClick.HasDelegate)
            {
                _clickEventRegistered = true;

                var button = Document.GetElementByReference(Ref);
                await button.AddEventListenerAsync("click", CreateEventCallback<MouseEventArgs>(HandleOnClick), false, new EventListenerExtras
                {
                    PreventDefault = OnClickPreventDefault,
                    StopPropagation = OnClickStopPropagation
                });
            }
        }

        private void InitIcon()
        {
            var builder = new RenderTreeBuilder();
            ChildContent(builder);

#pragma warning disable BL0006 // Do not use RenderTree types
            var frames = builder.GetFrames().Array;
            //todo Array will change
            var frame = builder.GetFrames().Array
                               .FirstOrDefault(u => u.FrameType == RenderTreeFrameType.Text || u.FrameType == RenderTreeFrameType.Markup);

            char[] charsToTrim = { '\r', ' ', '\n' };
            Icon = frame.TextContent.Trim(charsToTrim);
            //is material icons
            if (Icon.IndexOf("-") <= -1 && !RegexHelper.RegexSvgPath(Icon))
            {
                NewChildren = Icon;
            }
            else
            {
                NewChildren = string.Empty;
            }
#pragma warning restore BL0006 // Do not use RenderTree types
        }
    }
}