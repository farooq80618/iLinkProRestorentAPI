using Dapper;
using iLinkProRestorentAPI.ApplicationSettings.Email;
using iLinkProRestorentAPI.Context;
using iLinkProRestorentAPI.DTO;
using iLinkProRestorentAPI.Enums;
using iLinkProRestorentAPI.Interfaces;
using iLinkProRestorentAPI.Model;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Drawing;

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
                    DIRate as DineinRate ,
                    IsNull((Select Top 1 VAT from Category Where CategoryName = @Category) , 0) VAT , 
                    IsNull((Select Top 1 ST from Category Where CategoryName = @Category) , 0) ST , 
                    IsNull((Select Top 1 SC from Category Where CategoryName = @Category) , 0) SC , 
                case 
                    When (Select COUNT(*) from Modifiers m Where Dish.DishName = m.Item) > 0 then 1
                    else 0 
                    end as ModifierFlag
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
                    IsNull((Select Top 1 VAT from Category Where CategoryName = @Category) , 0) VAT , 
                    IsNull((Select Top 1 ST from Category Where CategoryName = @Category) , 0) ST , 
                    IsNull((Select Top 1 SC from Category Where CategoryName = @Category) , 0) SC , 
                    Rate , 0 as ModifierFlag
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

        public async Task<Tuple<int, string, List<Modifiers>>> GetModifireAsync(string Dish)
        {
            try
            {
                var modifiers = new List<Modifiers>();

                using (var connection = _context.CreateConnection())
                {
                    var modifierQuery = $"Select * from Modifiers Where Item = '{Dish}'";
                    var modifier = (await connection.QueryAsync<Modifiers>(modifierQuery)).ToList();

                    return Tuple.Create((int)ApplicationEnum.APIStatus.Success, "Success", modifier);

                }
            }
            catch (Exception ex)
            {
                return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, ex.Message, (List<Modifiers>)null);
            }
        }

        public async Task<Tuple<int, string, List<PizzaSize>>> GetPizzaAsync()
        {
            try
            {
                var pizzaSizeQuery = "SELECT SizeID, RTRIM(Size) AS Size FROM PizzaSize";

                var pizzaMasterQuery = @"
                SELECT 
                    Pizza_ID, 
                    RTRIM(PizzaName) AS PizzaName, 
                    RTRIM(PizzaSize) AS PizzaSize, 
                    [Description] AS Desription,
                    Rate,
                    ToppingsLimit,
                    Discount ,
                     IsNull((Select IsNull(VAT , 0) as VAT from Category Where CategoryName = 'PIZZA') , 0) as VAT,
                     IsNull((Select IsNull(ST , 0) as ST from Category Where CategoryName = 'PIZZA') , 0) as ST,
                     IsNull((Select IsNull(SC , 0) as SC from Category Where CategoryName = 'PIZZA') , 0) as SC
                FROM PizzaMaster";

                var pizzaModifierQuery = @"
                SELECT 
                    PM_ID, 
                    PizzaID, 
                    RTRIM(ModifierName) AS ModifierName, 
                    Rate 
                FROM PizzaModifier";

                var pizzaToppingQuery = @"
                SELECT 
                    T_ID, 
                    RTRIM(ToppingName) AS ToppingName, 
                    RTRIM(ToppingSize) AS ToppingSize,
                    RTRIM(PizzaSize) AS PizzaSize, 
                    Rate 
                FROM PizzaTopping";

                using (var connection = _context.CreateConnection())
                {
                    var sizes = (await connection.QueryAsync<PizzaSize>(pizzaSizeQuery)).ToList();
                    var pizzas = (await connection.QueryAsync<PizzaMaster>(pizzaMasterQuery)).ToList();
                    var modifiers = (await connection.QueryAsync<PizzaModifier>(pizzaModifierQuery)).ToList();
                    var toppings = (await connection.QueryAsync<PizzaTopping>(pizzaToppingQuery)).ToList();

                    foreach (var pizza in pizzas)
                    {
                        pizza.Modifier = modifiers
                            .Where(m => m.PizzaID == pizza.Pizza_ID)
                            .ToList();
                    }
                    foreach (var size in sizes)
                    {
                        size.PizzaMaster = pizzas
                            .Where(p => p.PizzaSize?.Equals(size.Size, StringComparison.OrdinalIgnoreCase) == true)
                            .ToList();

                        size.PizzaTopping = toppings
                            .Where(t => t.PizzaSize?.Equals(size.Size, StringComparison.OrdinalIgnoreCase) == true)
                            .ToList();
                    }

                    return Tuple.Create(
                        (int)ApplicationEnum.APIStatus.Success,
                        "Success",
                        sizes
                    );
                }
            }
            catch (Exception ex)
            {
#pragma warning disable CS8619
                return Tuple.Create(
                    (int)ApplicationEnum.APIStatus.Failed,
                    ex.Message,
                    (List<PizzaSize>)null
                );
#pragma warning restore CS8619
            }
        }

        public async Task<Tuple<int, string, bool>> InsertOrderAsync(OrderMaster order)
        {
            using var connection = _context.CreateConnection();
            if(connection.State == System.Data.ConnectionState.Closed) 
                connection.Open();  

            using var transaction = connection.BeginTransaction();

            try
            {
                // --- 1️⃣ Basic Validations ---
                if (string.IsNullOrWhiteSpace(order.Table))
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "Table not selected", false);

                if (order.orderDetails == null || !order.orderDetails.Any())
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "No items added", false);

                // --- 2️⃣ Validate stock availability (Temp_Stock_Store) ---
                foreach (var group in order.orderDetails.GroupBy(o => o.Dish))
                {
                    string sqlQty = "SELECT Qty FROM Temp_Stock_Store WHERE Dish = @Dish";
                    decimal availableQty = await connection.ExecuteScalarAsync<decimal?>(sqlQty, new { Dish = group.Key }, transaction) ?? 0;
                    decimal requestedQty = group.Sum(x => x.Quantity);

                    if (requestedQty > availableQty)
                    {
                        transaction.Rollback();
                        return Tuple.Create((int)ApplicationEnum.APIStatus.Failed,
                            $"Added qty ({requestedQty}) more than available ({availableQty}) for '{group.Key}'", false);
                    }
                }

                // --- 3️⃣ Deduct stock for each dish in Temp_Stock_Store ---
                foreach (var item in order.orderDetails)
                {
                    string updateStock = @"UPDATE Temp_Stock_Store SET Qty = Qty - @Qty WHERE Dish = @Dish";
                    await connection.ExecuteAsync(updateStock, new { Dish = item.Dish, Qty = item.Quantity }, transaction);
                }

                // --- Get next available ID ---
                string getMaxIdQuery = "SELECT ISNULL(MAX(ID), 0) + 1 FROM RestaurantPOS_OrderInfoKOT";
                int nextId = await connection.ExecuteScalarAsync<int>(getMaxIdQuery, transaction: transaction);

                // --- Get next TicketNo ---
                string getMaxTicketQuery = "SELECT ISNULL(MAX(CAST(SUBSTRING(TicketNo, 5, LEN(TicketNo) - 4) AS INT)), 0) + 1 FROM RestaurantPOS_OrderInfoKOT WHERE TicketNo LIKE 'KOT-%'";
                int nextTicketNo = await connection.ExecuteScalarAsync<int>(getMaxTicketQuery, transaction: transaction);

                string newTicketNo = $"KOT-{nextTicketNo}";

                // --- Insert Order Info ---
                string insertOrderInfo = @"
                 INSERT INTO RestaurantPOS_OrderInfoKOT 
                 (ID, TicketNo, BillDate, GrandTotal, TableNo, Operator, GroupName, TicketNote, KOT_Status, TaxType, NoOfPerson)
                 VALUES (@ID, @TicketNo, @BillDate, @GrandTotal, @TableNo, @Operator, @GroupName, @Notes, 'Open', @TaxType, @NoOfPerson);
                 SELECT CAST(SCOPE_IDENTITY() as int);
                ";

                int ticketId = await connection.ExecuteScalarAsync<int>(
                    insertOrderInfo,
                    new
                    {
                        ID = nextId,
                        TicketNo = newTicketNo,
                        BillDate = DateTime.Now,
                        GrandTotal = order.TotalAmount,
                        TableNo = order.Table,
                        Operator = order.Operator,
                        GroupName = order.orderType.ToString(),
                        Notes = order.Notes,
                        TaxType = "Exclusive",
                        NoOfPerson = order.NoOfPerson ?? 0
                    },
                    transaction
                );

                ticketId = nextId;
                // --- 5️⃣ Insert Ordered Items ---
                string insertOrderedItems = @"
                 INSERT INTO RestaurantPOS_OrderedProductKOT
                 (TicketID, Dish, Rate, Quantity, Amount, DiscountPer, DiscountAmount, STPer, STAmount, VATPer, VATAmount, SCPer, SCAmount, TotalAmount, Notes, Category, T_Number, ItemStatus , isComboDeal)
                 VALUES
                 (@TicketID, @Dish, @Rate, @Quantity, @Amount, @DiscountPer, @DiscountAmount, @STPer, @STAmount, @VATPer, @VATAmount, @SCPer, @SCAmount, @TotalAmount, @Notes, @Category, @TableNo, @ItemStatus , @isComboDeal);
                ";

                foreach (var item in order.orderDetails)
                {
                    await connection.ExecuteAsync(insertOrderedItems, new
                    {
                        TicketID = ticketId,
                        Dish = item.Dish,
                        Rate = item.Rate,
                        Quantity = item.Quantity,
                        Amount = item.Amount,
                        DiscountPer = item.DiscountPer,
                        DiscountAmount = item.DiscountAmount,
                        STPer = item.STPer,
                        STAmount = item.STAmount,
                        VATPer = item.VATPer,
                        VATAmount = item.VATAmount,
                        SCPer = item.SCPer,
                        SCAmount = item.SCAmount,
                        TotalAmount = item.Amount,
                        Notes = item.Notes,
                        Category = item.Category,
                        TableNo = order.Table,
                        ItemStatus = "New",
                        isComboDeal =  item?.isComboDeal ?? false,
                    }, transaction);
                }

                string updateTableColor = "UPDATE R_Table SET BkColor = @Color WHERE TableNo = @TableNo";
                await connection.ExecuteAsync(updateTableColor, new { Color = Color.Red.ToArgb(), TableNo = order.Table }, transaction);

                transaction.Commit();
                return Tuple.Create((int)ApplicationEnum.APIStatus.Success, "Order inserted successfully", true);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, ex.Message, false);
            }
        }

        public async Task<Tuple<int, string, bool>> InsertTakeAwayOrderAsync(OrderMaster order)
        {
            using var connection = _context.CreateConnection();
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                if (order.orderDetails == null || !order.orderDetails.Any())
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "No items added", false);

                foreach (var group in order.orderDetails.GroupBy(o => o.Dish))
                {
                    string sqlQty = "SELECT Qty FROM Temp_Stock_Store WHERE Dish = @Dish";
                    decimal availableQty = await connection.ExecuteScalarAsync<decimal?>(sqlQty, new { Dish = group.Key }, transaction) ?? 0;
                    decimal requestedQty = group.Sum(x => x.Quantity);

                    if (requestedQty > availableQty)
                    {
                        transaction.Rollback();
                        return Tuple.Create((int)ApplicationEnum.APIStatus.Failed,
                            $"Added qty ({requestedQty}) exceeds available ({availableQty}) for '{group.Key}'", false);
                    }
                }

                foreach (var item in order.orderDetails)
                {
                    string updateStock = "UPDATE Temp_Stock_Store SET Qty = Qty - @Qty WHERE Dish = @Dish";
                    await connection.ExecuteAsync(updateStock, new { Dish = item.Dish, Qty = item.Quantity }, transaction);
                }

                string getMaxIdQuery = "SELECT ISNULL(MAX(ID), 0) + 1 FROM RestaurantPOS_BillingInfoTA";
                int nextBillId = await connection.ExecuteScalarAsync<int>(getMaxIdQuery, transaction: transaction);

                string newBillNo = $"TA-{nextBillId.ToString("0000")}";

                string getMaxODNoQuery = "SELECT ISNULL(MAX(ODNo), 0) + 1 FROM tblOrder";
                int nextODNo = await connection.ExecuteScalarAsync<int>(getMaxODNoQuery, transaction: transaction);

                string insertOrder = "INSERT INTO tblOrder (ODNo, BillNo) VALUES (@ODNo, @BillNo)";
                await connection.ExecuteAsync(insertOrder, new { ODNo = nextODNo , BillNo = newBillNo }, transaction);

                string getNextRmIdQuery = "SELECT ISNULL(MAX(RM_ID), 0) + 1 FROM RM_Used";
                int nextRmId = await connection.ExecuteScalarAsync<int>(
                                    getNextRmIdQuery,
                                    param: null,
                                    transaction: transaction
                                );

                string insertRM = "INSERT INTO RM_Used (RM_ID, BillDate, BillNo) VALUES (@RM_ID, @BillDate, @BillNo)";
                await connection.ExecuteAsync(insertRM, new { RM_ID = nextRmId, BillDate = DateTime.Now, BillNo = newBillNo }, transaction);

                foreach (var item in order.orderDetails)
                {
                    string recipeQuery = @"
                        SELECT RJ.ProductID, RJ.Quantity
                        FROM Recipe R
                        INNER JOIN Recipe_Join RJ ON R.R_ID = RJ.RecipeID
                        WHERE R.Dish = @Dish";
                    var recipeItems = await connection.QueryAsync(recipeQuery, new { Dish = item.Dish }, transaction);

                    foreach (var rm in recipeItems)
                    {
                        decimal usedQty = (decimal)rm.Quantity * item.Quantity;

                        string updateRMStock = "UPDATE Temp_Stock_RM SET Qty = Qty - @Qty WHERE ProductID = @ProductID";
                        await connection.ExecuteAsync(updateRMStock, new { Qty = usedQty, ProductID = rm.ProductID }, transaction);

                        string insertRMJoin = "INSERT INTO RM_Used_Join (RawMaterialID, ProductID, Quantity) VALUES (@RawMaterialID, @ProductID, @Quantity)";
                        await connection.ExecuteAsync(insertRMJoin, new { RawMaterialID = nextRmId, ProductID = rm.ProductID, Quantity = usedQty }, transaction);
                    }
                }

                string insertBilling = @"
                    INSERT INTO RestaurantPOS_BillingInfoTA
                    (ID, BillNo, BillDate, GrandTotal, Cash, Change, Operator, SubTotal, ParcelCharges,
                     PaymentMode, BillNote, ExchangeRate, CurrencyCode, TADiscountPer, TADiscountAmt,
                     Member_ID, PhoneNo, ODN, TA_Status, GiftCardID, GiftCardAmount, LP, LA,
                     CustomerName, TaxType, Tip, RoundOff)
                    VALUES
                    (@ID, @BillNo, @BillDate, @GrandTotal, @Cash, @Change, @Operator, @SubTotal, @ParcelCharges,
                     @PaymentMode, @BillNote, @ExchangeRate, @CurrencyCode, @TADiscountPer, @TADiscountAmt,
                     @Member_ID, @PhoneNo, @ODN, 'Unpaid', @GiftCardID, @GiftCardAmount, @LP, @LA,
                     @CustomerName, @TaxType, @Tip, 0)";
                await connection.ExecuteAsync(insertBilling, new
                {
                    ID = nextBillId,
                    BillNo = newBillNo,
                    BillDate = DateTime.Now,
                    GrandTotal = order.TotalAmount,
                    Cash = order.TotalAmount,
                    Change = 0,
                    Operator = order.Operator,
                    SubTotal = order.SubTotal,
                    ParcelCharges = 0,
                    PaymentMode = order.PaymentMode ?? "Cash",
                    BillNote = order.Notes,
                    ExchangeRate = 1.00,
                    CurrencyCode = "USD",
                    TADiscountPer = 0,
                    TADiscountAmt = 0,
                    Member_ID = "",
                    PhoneNo = "",
                    ODN = newBillNo,
                    GiftCardID = "",
                    GiftCardAmount = 0.00,
                    LP = 0,
                    LA = 0.00,
                    CustomerName = "",
                    TaxType = "Exclusive",
                    Tip = 0
                }, transaction);

                // 9️⃣ Insert Order Details (RestaurantPOS_OrderedProductBillTA)
                string insertItems = @"
                    INSERT INTO RestaurantPOS_OrderedProductBillTA
                    (BillID, Dish, Rate, Quantity, Amount, DiscountPer, DiscountAmount,
                     STPer, STAmount, VATPer, VATAmount, SCPer, SCAmount, TotalAmount,
                     Notes, Category, ItemStatus)
                    VALUES
                    (@BillID, @Dish, @Rate, @Quantity, @Amount, @DiscountPer, @DiscountAmount,
                     @STPer, @STAmount, @VATPer, @VATAmount, @SCPer, @SCAmount, @TotalAmount,
                     @Notes, @Category, @ItemStatus)";
                foreach (var item in order.orderDetails)
                {
                    await connection.ExecuteAsync(insertItems, new
                    {
                        BillID = nextBillId,
                        Dish = item.Dish,
                        Rate = item.Rate,
                        Quantity = item.Quantity,
                        Amount = item.Amount,
                        DiscountPer = item.DiscountPer,
                        DiscountAmount = item.DiscountAmount,
                        STPer = item.STPer,
                        STAmount = item.STAmount,
                        VATPer = item.VATPer,
                        VATAmount = item.VATAmount,
                        SCPer = item.SCPer,
                        SCAmount = item.SCAmount,
                        TotalAmount = item.Amount,
                        Notes = item.Notes,
                        Category = item.Category,
                        ItemStatus = "Unpaid"
                    }, transaction);
                }

                transaction.Commit();
                return Tuple.Create((int)ApplicationEnum.APIStatus.Success, $"Takeaway order saved. BillNo: {newBillNo}", true);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, ex.Message, false);
            }
        }
    }
}