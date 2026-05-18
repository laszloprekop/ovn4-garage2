using Ovn4_GarageProject2;
using Ovn4_GarageProject2.Handler;
using Ovn4_GarageProject2.UI;

var handler = new GarageHandler();
var ui = new ConsoleUi(handler);
new Manager(ui).Run();
