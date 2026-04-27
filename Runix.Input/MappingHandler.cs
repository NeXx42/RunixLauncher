using Runix.Structure.Enums;

namespace Runix.Input;

public static class ControllerMappingHandler
{
    public static string GetControllerMapping(ControllerType controller)
    {
        switch (controller)
        {
            case ControllerType.PS4: return "03000000120c00000807000000000000,PS4 Controller,a:b1,b:b2,back:b8,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,guide:b12,leftshoulder:b4,leftstick:b10,lefttrigger:a3,leftx:a0,lefty:a1,rightshoulder:b5,rightstick:b11,righttrigger:a4,rightx:a2,righty:a5,start:b9,touchpad:b13,x:b0,y:b3,platform:Windows,";
        }

        return "";
    }
}
