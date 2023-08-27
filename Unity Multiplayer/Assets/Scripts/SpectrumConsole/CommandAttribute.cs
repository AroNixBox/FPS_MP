using System;

namespace SpectrumConsole
{
   [AttributeUsage(AttributeTargets.Method)]
   public class CommandAttribute : Attribute
   {
       public string CustomCommandName { get; private set; }
   
       public CommandAttribute(string customCommandName = null)
       {
           CustomCommandName = customCommandName;
       }
   } 
}

