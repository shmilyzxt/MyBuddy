using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Zeta.Common;
using Zeta.Internals;

namespace QuestTools
{
    public class GameUI
    {
        private const ulong mercenaryOKHash = 1591817666218338490;
        private const ulong conversationSkipHash = 0x942F41B6B5346714;

        public static UIElement MercenaryOKButton
        {
            get
            {
                if (UIElement.IsValidElement(mercenaryOKHash))
                    return UIElement.FromHash(mercenaryOKHash);
                else
                    return null;
            }
        }

        public static UIElement ConversationSkipButton
        {
            get
            {
                if (UIElement.IsValidElement(conversationSkipHash))
                    return UIElement.FromHash(conversationSkipHash);
                else
                    return null;
            }
        }

        public static bool IsElementVisible(UIElement element)
        {
            if (element == null)
                return false;
            if (!element.IsValid)
                return false;
            if (!element.IsVisible)
                return false;

            return true;
        }

        public static void SafeClickElement(UIElement element, string name = "")
        {
            if (IsElementVisible(element))
            {
                Logging.Write("[QuestTools] Clicking UI element {0} ({1})", name, element.BaseAddress);
                element.Click();
                Thread.Sleep(250);

            }
        }

        public static void SafeClickUIButtons()
        {
            SafeClickElement(MercenaryOKButton);
            SafeClickElement(ConversationSkipButton);
        }
    }
}
