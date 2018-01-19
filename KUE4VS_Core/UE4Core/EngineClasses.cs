// Copyright 2018 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KUE4VS
{
    public static class EngineTypes
    {
        public static readonly ReadOnlyCollection<UClassDefn> UClasses = new ReadOnlyCollection<UClassDefn>(
            new[] {
                // Core
                new UClassDefn("UObject", "Object.h"),
                new UClassDefn("AActor", "GameFramework/Actor.h"),
                
                // Components
                new UClassDefn("UActorComponent", "Components/ActorComponent.h"),
                new UClassDefn("USceneComponent", "Components/SceneComponent.h"),
                new UClassDefn("UPrimitiveComponent", "Components/PrimitiveComponent.h"),

                // Game Framework
                new UClassDefn("AGameModeBase", "GameFramework/GameModeBase.h"),
                new UClassDefn("AGameMode", "GameFramework/GameMode.h"),
                new UClassDefn("AGameStateBase", "GameFramework/GameStateBase.h"),
                new UClassDefn("AGameState", "GameFramework/GameState.h"),
                new UClassDefn("APlayerState", "GameFramework/PlayerState.h"),
                new UClassDefn("APawn", "GameFramework/Pawn.h"),
                new UClassDefn("ACharacter", "GameFramework/Character.h"),
                new UClassDefn("AController", "GameFramework/Controller.h"),
                new UClassDefn("APlayerController", "GameFramework/PlayerController.h"),
                new UClassDefn("AAIController", "AIController.h"),
                new UClassDefn("ACamera", "GameFramework/Camera.h"),
                new UClassDefn("AStaticMeshActor", "Engine/StaticMeshActor.h"),

                // Misc
                new UClassDefn("UDataAsset", "Engine/DataAsset.h"),
                new UClassDefn("UGameInstance", "Engine/GameInstance.h"),
                new UClassDefn("ALevelScriptActor", "Engine/LevelScriptActor.h"),
            });
    }
}
