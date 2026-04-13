import { Entity as E2 } from "cs2/utils";

export type Entity = {
    __Type?: 'Unity.Entities.Entity, Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null';
    Index: number;
    Version: number;
};

export function toEntityTyped(entity: E2): Entity {
    return {
        __Type: 'Unity.Entities.Entity, Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null',
        Index: entity.index,
        Version: entity.version
    }
}
export function toVanillaEntity(entity: Entity): E2 {
    return {
        index: entity.Index,
        version: entity.Version
    }
}