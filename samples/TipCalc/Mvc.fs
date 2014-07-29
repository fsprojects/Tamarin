﻿namespace global

open System
open System.ComponentModel

type IView<'Event, 'Model> = 
    abstract Events : IObservable<'Event> with get
    abstract SetBindings : 'Model -> unit

type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

type IController<'Event, 'Model> =
    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

[<AbstractClass>]
type Controller<'Event, 'Model>() =
    interface IController<'Event, 'Model> with
        member this.InitModel model = this.InitModel model
        member this.Dispatcher = this.Dispatcher

    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

    static member Create callback = {
        new IController<'Event, 'Model> with
            member __.InitModel _ = () 
            member __.Dispatcher = Sync << callback
    }

[<Sealed>]
type Mvc<'Event, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Event, 'Model>, controller : IController<'Event, 'Model>) =

    member this.Start() =
        controller.InitModel model
        view.SetBindings model

        view.Events.Subscribe( fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler -> eventHandler model
            | Async eventHandler -> Async.StartImmediate( eventHandler model)
        )

type Model() = 
    let propertyChanged = Event<_, _>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish 

    member this.NotifyPropertyChanged propertyName = 
        propertyChanged.Trigger(this, PropertyChangedEventArgs propertyName)

