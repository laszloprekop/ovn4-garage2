# Lecture 260515 – Collections – IEnumerable\<T\>

## IEnumerable\<T\>

`IEnumerable<T>` is a fundamental interface in .NET that enables **iteration** over a sequence of objects of type `T`. It is the key to `foreach` loops and LINQ queries.

---

### What is IEnumerable\<T\>?

Think of `IEnumerable<T>` as a "contract" that says: *"You can loop through this collection one item at a time."*

---

### Why use it?

We use `IEnumerable<T>` to provide **minimal (minimalistic) access** to data — **iteration only, no indexing or counting**.

We do nothing with the elements other than loop through them, e.g. for printing purposes.

It promotes **abstraction** so that your code works with any collection (`List`, `Array`, `Dictionary`) without caring about the underlying implementation.

It also enables LINQ methods such as `Where` and `Select`, and supports **deferred execution**, where the code only runs at the point of iteration. More on this later.

---

### When should we use it?

- **Return type in methods:** Return `IEnumerable<T>` for flexibility, e.g. in APIs.
- **Parameters:** Accept `IEnumerable<T>` to work with any collection type.
- **LINQ and streams:** For large datasets or streaming, since it does not load everything into memory at once.

Use `IEnumerable<T>` instead of `List<T>` when you want to keep your code **flexible**, **memory-friendly**, and **abstract** — it is an appropriate return type or parameter for read-only scenarios.

---

### When should we use IEnumerable\<T\>?

For **read-only access** and **deferred execution** (the code does not run until you iterate).

It is a great fit when you:

- Return data from methods (e.g. repository or LINQ queries) without exposing the implementation.
- Handle large datasets or streaming (e.g. from a database), since it does not load everything into memory immediately.

---

### When should we use List\<T\>?

When you need to **modify**, **index**, or use **Count**:

- Add or remove elements (`Add`, `Remove`).
- Fast access by index (`list[0]`).

---

## How can we use IEnumerable in a garage?

`IEnumerable<T>` is a step towards better design in a garage system, where you handle vehicles without loading everything into memory at once.

In our garage app (e.g. for vehicle inventory or repairs), `IEnumerable<T>` provides **lazy loading** and **abstraction**:

- We loop or filter without using a full `List`.
- Could be used for large lists from a database or files, such as "fetch today's repairs".

---

## More on how IEnumerable\<T\> works

### What does this code mean?

```csharp
public class Garage<T> : IEnumerable<T> where T : Vehicle
```

Here we define a **generic** class `Garage<T>` that may only contain types that inherit from `Vehicle`, and the class can be used in `foreach` because it implements `IEnumerable<T>`.

- **`Garage<T>`:** `T` is a type parameter — a placeholder for a type that is determined when the class is used.
- **`: IEnumerable<T>`:** The class can be iterated, for example with `foreach`.
- **`where T : Vehicle`:** Only types that are a `Vehicle` or inherit from `Vehicle` are allowed.

### What does this mean in practice?

This is useful when we want a garage that can be specialized, for example:

```csharp
Garage<Car> carGarage = new Garage<Car>();
Garage<Motorcycle> bikeGarage = new Garage<Motorcycle>();
```

The compiler then knows that every object in the garage is a variant of `Vehicle`, which provides better type safety.

---

### The GetEnumerator implementation

You will be using this code:

```csharp
public IEnumerator<T> GetEnumerator() =>
    _vehicles.OfType<T>().GetEnumerator();
```

This takes `_vehicles`, filters out only the objects that are actually of type `T` using `OfType<T>()`, and then returns an enumerator over them.

This means that if `_vehicles` contains several different vehicle types, we only get the ones matching `T` when we loop through the collection.

---

## Garage structure

The application is composed of the following layers:

```
Program → Manager ↔ UI
                       ↘
                        Garage
                        
Vehicle
├── Car
└── Airplane
```

The `Manager` communicates with both the `UI` and the `Garage`. The `UI` interacts with the user and delegates operations to the `Manager`.

### Loose coupling with interfaces

By introducing interfaces, we achieve **loose coupling**: we program against interfaces rather than concrete classes.

```
Program → Manager ↔ IHandler / Handler → IGarage / Garage
                 ↗
              IUI / UI
```

This makes the code easier to swap out, test, and extend, because `Manager` does not need to care about the exact internal implementation of `UI` or `Garage`.
