using CefSharp.OutOfProcess.Interface;

namespace CefSharp.OutOfProcess.Internal
{
    public interface IRenderHandlerInternal
    {
        void OnPaint(bool isPopup, Rect dirtyRect, int width, int height, string file);

        void OnPopupShow(bool show);

        void OnPopupSize(Rect rect);
    }
}