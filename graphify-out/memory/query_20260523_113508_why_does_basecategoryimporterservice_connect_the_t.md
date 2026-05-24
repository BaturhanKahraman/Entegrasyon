---
type: "query"
date: "2026-05-23T11:35:08.562904+00:00"
question: "Why does BaseCategoryImporterService connect the Trendyol, Hepsiburada, and N11 category importers through ICategoryImporterService?"
contributor: "graphify"
source_nodes: ["BaseCategoryImporterService", "ICategoryImporterService", "TrendyolCategoryImporter", "HepsiburadaCategoryImporter", "N11CategoryImporter"]
---

# Q: Why does BaseCategoryImporterService connect the Trendyol, Hepsiburada, and N11 category importers through ICategoryImporterService?

## Answer

Template Method pattern. ICategoryImporterService (Abstract) defines the contract (Source, GetExternalCategoriesAsync, ImportCategoriesAsync, ImportCategoryAsync) + neutral DTOs. BaseCategoryImporterService is an abstract class implementing it, owning shared machinery: transaction-wrapped ImportCategoriesAsync (L48), recursive tree walk ImportCategoryInternalAsync (L98), LoadMarketPlaceAsync (L32), CreateMarketplaceLinkAsync (L172). Abstract holes Source (L21) + GetExternalCategoriesAsync (L42); virtual hooks ImportCategoryAttributesAsync (L198). Three concretes (Trendyol L19 HttpClient/REST, Hepsiburada L20 IHepsiburadaApiClient, N11 L15 IN11SoapClient/SOAP) override only the fetch; each sets its Source. The base stamps every DB row with ImportSource==Source (L109/L131) - the polymorphic glue making it the betweenness hub across communities 66/67/129/254. Runtime dispatch is NOT via the interface: string switch in CategoryImportBackgroundService.cs L51-53 + concrete DI registration (ApplicationDependencyExtension L81-82,112) + direct concrete injection in CategoryImport.razor.cs. So the base class, not the interface, is the actual reuse seam.

## Source Nodes

- BaseCategoryImporterService
- ICategoryImporterService
- TrendyolCategoryImporter
- HepsiburadaCategoryImporter
- N11CategoryImporter