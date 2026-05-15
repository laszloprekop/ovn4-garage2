# C# Exercise 4 – Garage 2.0

> **Note:** The result of the exercise must be demonstrated to and approved by the teacher before it can be considered complete.

---

## An initial overarching project

To tie together much of what you have learned, we will now build a garage application. This application should provide the functionality that a system might need if it is to be used to simulate a simple garage. It should be possible to park vehicles, retrieve vehicles, see which vehicles are present and what properties they have — all within a console application with a main menu and sub-menus.

The reason you will be programming a garage is that it makes the separation of concerns easy to anchor. We can primarily divide a garage into the following parts:

**The Garage:** A representation of the physical building. The garage is a place where a collection of vehicles can be stored. The garage can therefore be represented as a collection of vehicles.

**Vehicles:** Cars, motorcycles, unicycles, or whatever type of vehicle you want to park in the garage.

These are the two "object types" you encounter in a physical garage. But looking more closely, there should also be subclasses of vehicle — each vehicle type is its own subclass in the system. In addition, functionality is required that handles parking vehicles in the garage, retrieving vehicles from the garage, and presenting and searching the contents of the garage.

In more programming-friendly terms, we need **at minimum**:

- A *collection* of vehicles: the class **`Garage`**.
- A vehicle class: the class **`Vehicle`**.
- A number of subclasses of `Vehicle`.
- A user interface that lets us use the garage's functionality. All user interaction happens here.
- A **`GarageHandler`**. To abstract a layer so that there is no direct contact between the user interface and the `Garage` class. This is appropriately done through a class that handles the functionality the interface needs access to.
- We do not program directly against concrete types, so we use interfaces: e.g. **`IUI`**, **`IHandler`**, **`IVehicle`**. (Tip: extract to an interface once the implementation is complete if you find this part difficult.)

---

## Requirements Specification

### Vehicles

Vehicles shall be implemented as the class `Vehicle` and its subclasses.

- `Vehicle` contains all properties that must exist across all vehicle types, e.g. registration number, colour, number of wheels, and any other properties you can think of.
- The registration number is **unique**.
- The following subclasses must exist at minimum:
  - `Airplane`
  - `Motorcycle`
  - `Car`
  - `Bus`
  - `Boat`
- Each subclass must implement at least one property of its own, for example:
  - Number of engines
  - Cylinder volume
  - Fuel type (Gasoline / Diesel)
  - Number of seats
  - Length

### The Garage class

The garage itself shall be implemented as a **generic collection** of vehicles:

```csharp
class Garage<T>
```

The generic type must also be constrained:

```csharp
class Garage<T> where ....
```

Furthermore, it must be possible to iterate over an instance of `Garage` using `foreach`. This means `Garage` must implement the generic variant of the `IEnumerable` interface:

```csharp
class Garage<T> : ....
```

The class does not need to inherit from any other class or implement any other interface.

The vehicle collection must be handled **internally as an array**. The internal array must be **private**. When instantiating a new garage, the capacity must be passed as an argument to the constructor.

> **We must NOT use `List<Vehicle>` internally in the `Garage` class!**

---

## Functionality

The following must be possible:

- List all parked vehicles.
- List vehicle types and how many of each are in the garage.
- Add and remove vehicles from the garage.
- Set a capacity (number of parking spaces) when instantiating a new garage.
- Populate the garage with a number of vehicles on startup.
- Find a specific vehicle by registration number. It must work regardless of casing — e.g. `ABC123`, `Abc123`, or `AbC123`.
- Search for vehicles based on one or more properties (all possible combinations from the base class `Vehicle`). For example:
  - All black vehicles with four wheels.
  - All pink motorcycles with 3 wheels.
  - All trucks.
  - All red vehicles.
- The user must receive feedback on whether actions succeeded or failed. For example, when parking a vehicle the user should get a confirmation that the vehicle is parked. If it fails, the user should be told why.

The program must be a **console application** with a text-based user interface. From the interface it must be possible to:

- Navigate to all garage functionality via the interface.
- Create a garage with a user-specified size.
- Shut down the application from the interface.

The application must handle input **robustly**, so that it does not crash on invalid input or usage.

---

## Optional Extra Functionality (not required)

- Ability to also search on vehicle-specific properties.
- Handle multiple garages that can hold different types of vehicles, e.g. a hangar, a regular garage, and a motorcycle garage.
  - The user can navigate between the different garages in the UI to perform the above-mentioned functions on only the currently selected garage.
  - The currently active garage must be clearly displayed.
- A garage no longer consists of vehicles but of parking spaces, which in turn can hold vehicles.
- Read and write to the filesystem from your application. Figure out how to save your garage (via menu or automatically on shutdown) and load your garage (via menu or automatically on startup).
- Different vehicles take up different amounts of space: a car takes 1 space, a boat takes 2 spaces, an airplane requires 3 spaces, a motorcycle takes only 1/3 of a space.
- When parking, only vehicle types that fit in the garage should be shown as options.
- Read the garage size from a configuration file.
- Any other functionality you think should be there.

**Good luck!**
