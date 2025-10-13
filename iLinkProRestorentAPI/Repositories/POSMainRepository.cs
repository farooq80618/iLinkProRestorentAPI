using Dapper;
using iLinkProRestorentAPI.ApplicationSettings.Email;
using iLinkProRestorentAPI.Context;
using iLinkProRestorentAPI.Enums;
using iLinkProRestorentAPI.Interfaces;
using iLinkProRestorentAPI.Model;

namespace iLinkProRestorentAPI.Repositories
{
    public class POSMainRepository : IPOSMainRepository
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;
        private readonly GenerateEmail _generateEmail;

        public POSMainRepository(DapperContext context, IConfiguration configuration, GenerateEmail generateEmail)
        {
            _generateEmail = generateEmail;
            _context = context;
            _configuration = configuration;
        }

        public async Task<Tuple<int, string, List<CategoryDTO>>> GetCategoryAsync(string? filter)
        {
            try
            {
                string strFilter = "";
                if(filter != null) 
                    if(Convert.ToString(filter).Length > 0)
                        strFilter = $" And Category.CategoryName like '%{filter}%' ";
                var registrationQuery = @$"
                    SELECT DISTINCT 
                          RTRIM(Category.CategoryName) AS CategoryName, 
                          Category.Cat_ID, 
                          ISNULL(Category.Position, Category.Cat_ID) AS Position  , 
                          (Select COUNT(*) from Dish Where Category  = Category.CategoryName And MI_Status = 'Active              ' ) as DishCount
                      FROM Category
                      LEFT JOIN CategoryDays 
                          ON Category.CategoryName = CategoryDays.CategoryName
                      WHERE 
                          Category.CategoryName <> 'Pizza' 
                          {strFilter}
                          AND Category.CatStatus = 'Active'  
                          AND Category.CategoryName <> 'Taxable/Non-Taxable'
                          AND (
                          CategoryDays.CategoryName IS NULL
                          OR CategoryDays.Deactive = 'Yes'
                          OR (
                              CategoryDays.Deactive = 'No'
                              AND (
                                  (
                                      (CategoryDays.Day IS NULL OR CategoryDays.Routine = 'Daily') 
                                      AND GETDATE() BETWEEN 
                                          CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' ' + CONVERT(VARCHAR(5), ISNULL(CategoryDays.StartTime, '00:00:00'), 108) AS DATETIME)
                                          AND 
                                          CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' ' + CONVERT(VARCHAR(5), ISNULL(CategoryDays.EndTime, '23:59:59'), 108) AS DATETIME)
                                  )
                                  OR
                                  (
                                      CategoryDays.Day = DATENAME(WEEKDAY, GETDATE())
                                      AND GETDATE() BETWEEN 
                                          CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' ' + CONVERT(VARCHAR(5), ISNULL(CategoryDays.StartTime, '00:00:00'), 108) AS DATETIME)
                                          AND 
                                          CAST(CONVERT(VARCHAR(10), GETDATE(), 120) + ' ' + CONVERT(VARCHAR(5), ISNULL(CategoryDays.EndTime, '23:59:59'), 108) AS DATETIME)
                                  )
                              )
                          )
                      )
                      ORDER BY 
                      Position ASC";

                using (var connection = _context.CreateConnection())
                {
                    var result = (await connection.QueryAsync<CategoryDTO>(registrationQuery)).ToList();

                    return Tuple.Create(
                        (int)ApplicationEnum.APIStatus.Success,
                        "Success",
                        result
                    );
                }
            }
            catch (Exception ex)
            {
                #pragma warning disable CS8619  
                return Tuple.Create(
                    (int)ApplicationEnum.APIStatus.Failed,
                    ex.Message,
                    (List<CategoryDTO>)null
                );
                #pragma warning restore CS8619 
            }
        }

        public async Task<Tuple<int, string, List<ProductDTO>>> GetProductAsync(string Category)
        {
            try
            {
                var registrationQuery = @$"
                SELECT 
                    RTRIM(DishName) ProductName,
                    Dish.BackColor,
                    RTRIM(Photo) ImageURL,
                    DishID,
                    RTRIM(FColor) ButtonColor,
                    MIPhoto SaveImageDB , 
                    DIRate as DineinRate
                FROM 
                    Category
                JOIN 
                    Dish ON Category.CategoryName = Dish.Category
                WHERE 
                    Category = @Category
                    AND MI_Status = 'Active'
                
                UNION ALL
                
                SELECT 
                    RTRIM(ComboName) AS ProductName,
                    -16777056 AS BackColor,
                    NULL AS ImageURL,
                    Id AS DishID,
                    NULL AS ButtonColor,
                    NULL AS SaveImageDB ,
                    Rate
                FROM 
                    combo
                WHERE 
                    Active = 'Yes'
                    AND 
                    (
                        (DATENAME(WEEKDAY, GETDATE()) = 'Monday' AND Monday = 1)
                        OR (DATENAME(WEEKDAY, GETDATE()) = 'Tuesday' AND Tuesday = 1)
                        OR (DATENAME(WEEKDAY, GETDATE()) = 'Wednesday' AND Wednesday = 1)
                        OR (DATENAME(WEEKDAY, GETDATE()) = 'Thursday' AND Thursday = 1)
                        OR (DATENAME(WEEKDAY, GETDATE()) = 'Friday' AND Friday = 1)
                        OR (DATENAME(WEEKDAY, GETDATE()) = 'Saturday' AND Saturday = 1)
                        OR (DATENAME(WEEKDAY, GETDATE()) = 'Sunday' AND Sunday = 1)
                    )
                    AND CAST(GETDATE() AS TIME) BETWEEN CAST(TimeFrom AS TIME) AND CAST(TimeTo AS TIME)
                    And Category = @Category 
                ORDER BY 4";

                using (var connection = _context.CreateConnection())
                {
                    var result = (await connection.QueryAsync<ProductDTO>(registrationQuery, new { Category })).ToList();

                    return Tuple.Create(
                        (int)ApplicationEnum.APIStatus.Success,
                        "Success",
                        result
                    );
                }
            }
            catch (Exception ex)
            {
                #pragma warning disable CS8619
                return Tuple.Create(
                    (int)ApplicationEnum.APIStatus.Failed,
                    ex.Message,
                    (List<ProductDTO>)null
                );
                #pragma warning restore CS8619
            }
        }

        public async Task<Tuple<int, string, List<TableMaster>>> GetTablesAsync()
        {
            try
            {
                var tableMasters = new List<TableMaster>();

                using (var connection = _context.CreateConnection())
                {
                    var floorQuery = "SELECT DISTINCT RTRIM(FloorNo) AS FloorNo FROM R_Table ORDER BY FloorNo";
                    var floors = (await connection.QueryAsync<string>(floorQuery)).ToList();

                    var occupiedTableQuery = @"
                        SELECT DISTINCT RTRIM(TableNo) AS TableNo
                        FROM RestaurantPOS_BillingInfoKOT AS b
                        INNER JOIN RestaurantPOS_OrderedProductBillKOT AS o ON b.Id = o.BillID
                        WHERE DIB_Status = 'Unpaid'";
                    var occupiedTables = (await connection.QueryAsync<string>(occupiedTableQuery)).Select(t => t.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var floor in floors)
                    {
                        var tableQuery = @"
                            SELECT RTRIM(TableNo) AS TableName, 6 AS TableCapacityCount
                            FROM R_Table
                            WHERE FloorNo = @Floor";

                        var tablesRaw = await connection.QueryAsync<Table>(tableQuery, new { Floor = floor });

                        // Step 4: Determine occupancy
                        var tablesWithStatus = tablesRaw.Select(t => new Table
                        {
                            TableName = t.TableName,
                            TableCapacityCount = t.TableCapacityCount,
                            IsOccupied = occupiedTables.Contains(t.TableName)
                        }).ToList();

                        tableMasters.Add(new TableMaster
                        {
                            Floor = floor,
                            tables = tablesWithStatus
                        });
                    }
                }

                return Tuple.Create((int)ApplicationEnum.APIStatus.Success, "Success", tableMasters);
            }
            catch (Exception ex)
            {
                return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, ex.Message, (List<TableMaster>)null);
            }
        }


    }
}