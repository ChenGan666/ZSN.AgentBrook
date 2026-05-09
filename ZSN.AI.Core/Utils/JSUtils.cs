using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace ZSN.AI.Core.Utils
{
    public static class JSUtils
    {
        public static async Task ScrollToBottomAsync(this IJSRuntime _JSRuntime, string elementId)
        {
            await _JSRuntime.InvokeVoidAsync("scrollToBottom", elementId);
        }
    }
}
