using System;
using System.Windows;
using System.Windows.Threading;

namespace IceBloc.Utility;

public static class WpfExtensions
{
    private static readonly Action EmptyDelegate = delegate { };
    public static void Refresh(this UIElement uiElement)
    {
        uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
    }
}