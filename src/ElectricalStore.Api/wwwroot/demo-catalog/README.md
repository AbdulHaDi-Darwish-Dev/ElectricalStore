# Demo catalog media assets

Place consistent square ecommerce product photography here.

## Style requirements

- Professional ecommerce product photography
- Electrical product centered on white / very light gray background
- Soft realistic shadow
- Same camera perspective family
- Consistent object scale
- Square 1:1 (recommended 1200×1200)
- No baked-in text, logos, watermarks, people, or busy scenes

## Folder layout

```
wwwroot/demo-catalog/
  categories/
  products/
```

Public URL pattern (Development):

`http://localhost:5180/demo-catalog/{relative-path}`

The seeder attaches an image **only when the file exists**. Missing files leave the catalog row without media (storefront shows the empty-image state).

## Required category files

| File | Category |
|------|----------|
| categories/breakers.png | القواطع والحماية الكهربائية |
| categories/switches.png | المفاتيح والمقابس |
| categories/lighting.png | الإنارة والمصابيح |
| categories/cables.png | الأسلاك والكابلات |
| categories/panels.png | لوحات وصناديق التوزيع |
| categories/conduits.png | التمديدات والملحقات الكهربائية |
| categories/meters.png | أجهزة القياس والفحص |
| categories/power.png | مستلزمات الطاقة والحماية |
| categories/tools.png | الأدوات الكهربائية |
| categories/install.png | مستلزمات التركيب |

## Required product files

See `MANIFEST.txt` in this folder for the full deterministic filename list used by `DemoCatalogDefinitions`.

JPEG/PNG are also acceptable if you update filenames in the definitions, but WebP is preferred.
