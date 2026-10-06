using System;
using UnityEngine;

[Serializable]
public class PriceItem
{
    public string itemName;
    public Sprite image;
    public float price;
}

[CreateAssetMenu(fileName = "PriceCatalog", menuName = "Price Catalog")]
public class PriceCatalog : ScriptableObject
{
    public PriceItem[] items;
}
