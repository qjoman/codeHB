#Bank holidays
##Challenge description
1. Create an public (anonymous) Azure Function with HTTP trigger and an year parameter;
2. Retrieve holidays data from https://www.gov.uk/bank-holidays.json;
3. Filter the data using the given year;
4. Export the data as a csv in descending order.
##Code overview
The first thing to do is retrieve the holidays json from the the third party after checking if the year parameter is present and valid (more of it in error detection section)
```csharp
HttpClient newClient = new HttpClient();
HttpRequestMessage newRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.gov.uk/bank-holidays.json");

HttpResponseMessage response = await newClient.SendAsync(newRequest);
```
After that, the content is read and converted to dynamic object
```csharp
string jsonString = await response.Content.ReadAsStringAsync();
dynamic json = JsonConvert.DeserializeObject(jsonString);
```
For further usage, we can create a date range by defining the limits as the first and last day of that given year
```csharp
DateTime start = new DateTime(year_i, 1, 1);
DateTime end = new DateTime(year_i, 12, 31);
```
Type conversion to a list of an struct to make it easier to handle the data futher down in the code
```csharp
private struct HolidayEvent
{
   public string title;
   public string date;
   public string notes;
   public bool bunting;
}
...
List<HolidayEvent> data = json["england-and-wales"].events.ToObject<List<HolidayEvent>>();
```
Using a simple LINQ query it's possible to filter the csv by dates that are within the range of the start and end of the given year
```csharp
string ret = data.Where(x =>
{
DateTime date = DateTime.Parse(x.date);

return date >= start && date <= end;
})
```
The last part of the LINQ query is responsible for ordering the resulting data and formating the csv content
```csharp
.OrderByDescending(x => x.date)
.Select(x => string.Format("{0}, {1}", x.date, x.title))
.Aggregate((c, n) => string.Format("{0}\n{1}", c, n));
```
In the return, the code generates the csv download
```csharp
return new FileContentResult(Encoding.UTF8.GetBytes(ret), "application/octet-stream")
{
FileDownloadName = string.Format("england-and-wales-{0}.csv", year)
};
```
##Error detection and handling
There are a few places where errors can happen:
1. Invalid year param
   1.1 Year not present
```csharp
if (String.IsNullOrEmpty(year))
{
      log.LogInformation("Year parameter is required");

      return new BadRequestObjectResult("Year is required.");
}
```
   1.2 Invalid format
```csharp
int year_i = 0;
if (Int32.TryParse(year, out year_i))
{
   ...
}
else{
   log.LogInformation("Invalid year param. Not a valid number.");

   return new BadRequestObjectResult("Year is not a valid number.");
}
```
   1.3 Negative year
```csharp
if (year_i < 0)
{
   log.LogInformation("Invalid year. It should be not negative.");

   return new BadRequestObjectResult("Invalid year. It should be not negative.");
}
```
2. Requesting to UK holidays API, missing date or data from response (TODO)
```csharp
try
{
      HttpClient newClient = new HttpClient();
      HttpRequestMessage newRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.gov.uk/bank-holidays.json");

      HttpResponseMessage response = await newClient.SendAsync(newRequest);
      ....
}
catch (Exception e)
{
      //TODO: add exception treatment
      return new BadRequestObjectResult(e.Message);
}
```
##Testing
The function can be reached with the url 
```
[GET] https://qjoman.azurewebsites.net/api/BankHolidays?year=2016
```
The query param year can be changed to any value
##TODO
1. Add the rest of information (notes and bunting) if needed;
2. Treat the possible errors when reaching for the third party API;
3. Treat the possible errors if the "england-and-wales" data is not present;
4. Treat the possible errors if date is not present on the data;
5. Treat the possible errors if data is invalid (ex: warn the user about data format changed);
6. Check if using string comparsion/regex for the date filtering is faster than using string to DateTime conversion.