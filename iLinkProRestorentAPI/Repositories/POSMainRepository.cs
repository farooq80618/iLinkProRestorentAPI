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

        public async Task<Tuple<int, string, OrderResponse>> InsertOrderAsync(OrderMaster order)
        {
            using var connection = _context.CreateConnection();
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1️⃣ Basic validation
                if (string.IsNullOrWhiteSpace(order.Table))
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "Table not selected", (OrderResponse)null);

                if (order.orderDetails == null || !order.orderDetails.Any())
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "No items added", (OrderResponse)null);

                // 2️⃣ Validate stock availability
                foreach (var group in order.orderDetails.GroupBy(o => o.Dish))
                {
                    string sqlQty = "SELECT Qty FROM Temp_Stock_Store WHERE Dish = @Dish";
                    decimal availableQty = await connection.ExecuteScalarAsync<decimal?>(
                        sqlQty,
                        new { Dish = group.Key },
                        transaction: transaction
                    ) ?? 0;

                    decimal requestedQty = group.Sum(x => x.Quantity);

//                    if (requestedQty > availableQty)
//                    {
//                        transaction.Rollback();
//#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
//                        return Tuple.Create(
//                            (int)ApplicationEnum.APIStatus.Failed,
//                            $"Added qty ({requestedQty}) more than available ({availableQty}) for '{group.Key}'",
//                            item3: null as OrderResponse);
//#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
//                    }
                }

                // 3️⃣ Deduct stock
                string updateStock = "UPDATE Temp_Stock_Store SET Qty = Qty - @Qty WHERE Dish = @Dish";
                foreach (var item in order.orderDetails)
                {
                    await connection.ExecuteAsync(updateStock, new { Dish = item.Dish, Qty = item.Quantity }, transaction);
                }

                // 4️⃣ Generate new ticket info
                int nextId = await connection.ExecuteScalarAsync<int>(
                    "SELECT ISNULL(MAX(ID), 0) + 1 FROM RestaurantPOS_OrderInfoKOT",
                    transaction: transaction
                );

                int nextTicketNo = await connection.ExecuteScalarAsync<int>(
                    "SELECT ISNULL(MAX(CAST(SUBSTRING(TicketNo, 5, LEN(TicketNo) - 4) AS INT)), 0) + 1 FROM RestaurantPOS_OrderInfoKOT WHERE TicketNo LIKE 'KOT-%'",
                    transaction: transaction
                );

                string newTicketNo = $"KOT-{nextTicketNo:0000}";
                // 5️⃣ Insert main order
                string insertOrderInfo = @"
                    INSERT INTO RestaurantPOS_OrderInfoKOT 
                    (ID, TicketNo, BillDate, GrandTotal, TableNo, Operator, GroupName, TicketNote, KOT_Status, TaxType, NoOfPerson)
                    VALUES (@ID, @TicketNo, @BillDate, @GrandTotal, @TableNo, @Operator, @GroupName, @Notes, 'Open', @TaxType, @NoOfPerson);
                ";

                await connection.ExecuteAsync(insertOrderInfo, new
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
                }, transaction);

                // 6️⃣ Insert items
                string insertOrderedItems = @"
                    INSERT INTO RestaurantPOS_OrderedProductKOT
                    (TicketID, Dish, Rate, Quantity, Amount, DiscountPer, DiscountAmount, STPer, STAmount, VATPer, VATAmount, SCPer, SCAmount, TotalAmount, Notes, Category, T_Number, ItemStatus, isComboDeal)
                    VALUES
                    (@TicketID, @Dish, @Rate, @Quantity, @Amount, @DiscountPer, @DiscountAmount, @STPer, @STAmount, @VATPer, @VATAmount, @SCPer, @SCAmount, @TotalAmount, @Notes, @Category, @TableNo, @ItemStatus, @isComboDeal);
                ";

                foreach (var item in order.orderDetails)
                {
                    await connection.ExecuteAsync(insertOrderedItems, new
                    {
                        TicketID = nextId,
                        item.Dish,
                        item.Rate,
                        item.Quantity,
                        item.Amount,
                        item.DiscountPer,
                        item.DiscountAmount,
                        item.STPer,
                        item.STAmount,
                        item.VATPer,
                        item.VATAmount,
                        item.SCPer,
                        item.SCAmount,
                        TotalAmount = item.Amount,
                        item.Notes,
                        item.Category,
                        TableNo = order.Table,
                        ItemStatus = "New",
                        isComboDeal = item?.isComboDeal ?? false
                    }, transaction);
                }

                // 7️⃣ Update table status
                string updateTableColor = "UPDATE R_Table SET BkColor = @Color WHERE TableNo = @TableNo";
                await connection.ExecuteAsync(updateTableColor, new { Color = Color.Red.ToArgb(), TableNo = order.Table }, transaction);

                // 8️⃣ Commit transaction
                transaction.Commit();

                // 9️⃣ Build response
                var response = new OrderResponse
                {
                    OrderID = newTicketNo,
                    TableNo = order.Table,
                    TotalAmount = order.TotalAmount,
                    OrderTime = DateTime.Now
                };

                return Tuple.Create((int)ApplicationEnum.APIStatus.Success, "Order inserted successfully", response);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, ex.Message, (OrderResponse)null);
            }
        }

        public async Task<Tuple<int, string, OrderResponse>> InsertTakeAwayOrderAsync(OrderMaster order)
        {
            using var connection = _context.CreateConnection();
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1️⃣ Basic validation
                if (order.orderDetails == null || !order.orderDetails.Any())
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "No items added", (OrderResponse)null);

                // 2️⃣ Validate stock availability
                foreach (var group in order.orderDetails.GroupBy(o => o.Dish))
                {
                    const string sqlQty = "SELECT Qty FROM Temp_Stock_Store WHERE Dish = @Dish";
                    decimal availableQty = await connection.ExecuteScalarAsync<decimal?>(
                        sqlQty,
                        new { Dish = group.Key },
                        transaction: transaction
                    ) ?? 0;

                    decimal requestedQty = group.Sum(x => x.Quantity);

                    //if (requestedQty > availableQty)
                    //{
                    //    transaction.Rollback();
                    //    return Tuple.Create(
                    //        (int)ApplicationEnum.APIStatus.Failed,
                    //        $"Added qty ({requestedQty}) exceeds available ({availableQty}) for '{group.Key}'",
                    //        (OrderResponse)null
                    //    );
                    //}
                }

                // 3️⃣ Deduct stock
                const string updateStock = "UPDATE Temp_Stock_Store SET Qty = Qty - @Qty WHERE Dish = @Dish";
                foreach (var item in order.orderDetails)
                {
                    await connection.ExecuteAsync(updateStock, new { Dish = item.Dish, Qty = item.Quantity }, transaction);
                }

                // 4️⃣ Generate IDs and bill numbers
                const string getMaxIdQuery = "SELECT ISNULL(MAX(ID), 0) + 1 FROM RestaurantPOS_BillingInfoTA";
                int nextBillId = await connection.ExecuteScalarAsync<int>(getMaxIdQuery, transaction: transaction);

                string newBillNo = $"TA-{nextBillId:0000}";

                const string getMaxODNoQuery = "SELECT ISNULL(MAX(ODNo), 0) + 1 FROM tblOrder";
                int nextODNo = await connection.ExecuteScalarAsync<int>(getMaxODNoQuery, transaction: transaction);

                const string insertOrder = "INSERT INTO tblOrder (ODNo, BillNo) VALUES (@ODNo, @BillNo)";
                await connection.ExecuteAsync(insertOrder, new { ODNo = nextODNo, BillNo = newBillNo }, transaction);

                const string getNextRmIdQuery = "SELECT ISNULL(MAX(RM_ID), 0) + 1 FROM RM_Used";
                int nextRmId = await connection.ExecuteScalarAsync<int>(getNextRmIdQuery, transaction: transaction);

                const string insertRM = "INSERT INTO RM_Used (RM_ID, BillDate, BillNo) VALUES (@RM_ID, @BillDate, @BillNo)";
                await connection.ExecuteAsync(insertRM, new { RM_ID = nextRmId, BillDate = DateTime.Now, BillNo = newBillNo }, transaction);

                // 5️⃣ Update Raw Material Usage
                const string recipeQuery = @"
                    SELECT RJ.ProductID, RJ.Quantity
                    FROM Recipe R
                    INNER JOIN Recipe_Join RJ ON R.R_ID = RJ.RecipeID
                    WHERE R.Dish = @Dish";

                foreach (var item in order.orderDetails)
                {
                    var recipeItems = await connection.QueryAsync(recipeQuery, new { Dish = item.Dish }, transaction);

                    foreach (var rm in recipeItems)
                    {
                        decimal usedQty = (decimal)rm.Quantity * item.Quantity;

                        const string updateRMStock = "UPDATE Temp_Stock_RM SET Qty = Qty - @Qty WHERE ProductID = @ProductID";
                        await connection.ExecuteAsync(updateRMStock, new { Qty = usedQty, ProductID = rm.ProductID }, transaction);

                        const string insertRMJoin = "INSERT INTO RM_Used_Join (RawMaterialID, ProductID, Quantity) VALUES (@RawMaterialID, @ProductID, @Quantity)";
                        await connection.ExecuteAsync(insertRMJoin, new { RawMaterialID = nextRmId, ProductID = rm.ProductID, Quantity = usedQty }, transaction);
                    }
                }

                // 6️⃣ Insert Billing Info
                const string insertBilling = @"
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

                // 7️⃣ Insert Ordered Items
                const string insertItems = @"
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

                // 8️⃣ Commit
                transaction.Commit();

                // 9️⃣ Build structured response
                var response = new OrderResponse
                {
                    OrderID = newBillNo,
                    TableNo = "Takeaway",
                    TotalAmount = order.TotalAmount,
                    OrderTime = DateTime.Now
                };

                return Tuple.Create((int)ApplicationEnum.APIStatus.Success, $"Takeaway order saved successfully.", response);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, ex.Message, (OrderResponse)null);
            }
        }

        public async Task<Tuple<int, string, ViewOrderHistory>> ViewTakeAwayOrderHistoryAsync(string ticketNo)
        {
            using var connection = _context.CreateConnection();
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            try
            {
                // 1️⃣ Fetch billing info
                const string billingQuery = @"
                    SELECT 
                        BillNo,
                        Operator,
                        GrandTotal AS TotalAmount,
                        SubTotal,
                        PaymentMode,
                        BillNote AS Notes
                    FROM RestaurantPOS_BillingInfoTA
                    WHERE ID = @TicketNo";

                var billing = await connection.QueryFirstOrDefaultAsync(billingQuery, new { TicketNo = ticketNo });

                if (billing == null)
                    return Tuple.Create(
                        (int)ApplicationEnum.APIStatus.Failed,
                        "No takeaway order found for the provided Ticket ID.",
                        (ViewOrderHistory)null
                    );

                // 2️⃣ Fetch ordered items
                const string orderItemsQuery = @"
                    SELECT 
                        Dish,
                        Rate,
                        Quantity,
                        Amount,
                        VATPer,
                        VATAmount,
                        STPer,
                        STAmount,
                        SCPer,
                        SCAmount,
                        DiscountPer,
                        DiscountAmount,
                        Notes,
                        Category,
                        0 AS isComboDeal
                    FROM RestaurantPOS_OrderedProductBillTA
                    WHERE BillID = @TicketNo";

                var orderDetails = (await connection.QueryAsync<OrderDetailHistory>(orderItemsQuery, new { TicketNo = ticketNo })).ToList();

                // 3️⃣ Map to ViewOrderHistory object
                var result = new ViewOrderHistory
                {
                    Table = "Takeaway",
                    orderType = OrderType.TakeAway,
                    Operator = billing.Operator,
                    TotalAmount = billing.TotalAmount,
                    SubTotal = billing.SubTotal,
                    PaymentMode = billing.PaymentMode,
                    Notes = billing.Notes,
                    NoOfPerson = null,
                    orderDetails = orderDetails
                };

                return Tuple.Create(
                    (int)ApplicationEnum.APIStatus.Success,
                    "Takeaway order history fetched successfully.",
                    result
                );
            }
            catch (Exception ex)
            {
                #pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                return Tuple.Create(
                    (int)ApplicationEnum.APIStatus.Failed,
                    ex.Message,
                    (ViewOrderHistory)null
                );
                #pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
            }
        }

        public async Task<Tuple<int, string, ViewOrderHistory>> ViewKOTOrderHistoryAsync(string ticketNo)
        {
            using var connection = _context.CreateConnection();
            if (connection.State == ConnectionState.Closed)
                connection.Open(); // ✅ fixed

            try
            {
                const string orderQuery = @"
                    SELECT 
                        TicketNo,
                        TableNo AS [Table],
                        Operator,
                        GrandTotal AS TotalAmount,
                        NoOfPerson,
                        TaxType,
                        TicketNote AS Notes,
                        GroupName AS OrderType
                    FROM RestaurantPOS_OrderInfoKOT
                    WHERE RTRIM(TicketNo) = @TicketNo;";

                var orderInfo = await connection.QueryFirstOrDefaultAsync(orderQuery, new { TicketNo = ticketNo });

                if (orderInfo == null)
                {
                    return Tuple.Create(
                        (int)ApplicationEnum.APIStatus.Failed,
                        "No KOT order found for the given TicketNo.",
                        (ViewOrderHistory)null
                    );
                }

                const string orderItemsQuery = @"
                    SELECT 
                        Dish,
                        Rate,
                        Quantity,
                        Amount,
                        VATPer,
                        VATAmount,
                        STPer,
                        STAmount,
                        SCPer,
                        SCAmount,
                        DiscountPer,
                        DiscountAmount,
                        Notes,
                        Category,
                        CAST(ISNULL(isComboDeal, 0) AS bit) AS isComboDeal
                    FROM RestaurantPOS_OrderedProductKOT
                    WHERE TicketID = (
                        SELECT TOP 1 ID FROM RestaurantPOS_OrderInfoKOT WHERE RTRIM(TicketNo) = @TicketNo
                    );";

                var orderDetails = (await connection.QueryAsync<OrderDetailHistory>(orderItemsQuery, new { TicketNo = ticketNo })).ToList();

                // ✅ Explicitly declare type
                OrderType oType;
                Enum.TryParse<OrderType>(orderInfo.OrderType?.ToString() ?? "DineIn", out oType);

                var result = new ViewOrderHistory
                {
                    Table = orderInfo.Table,
                    orderType = oType,
                    Operator = orderInfo.Operator,
                    TotalAmount = orderInfo.TotalAmount,
                    SubTotal = orderInfo.TotalAmount,
                    PaymentMode = "N/A",
                    Notes = orderInfo.Notes,
                    NoOfPerson = orderInfo.NoOfPerson,
                    orderDetails = orderDetails
                };

                return Tuple.Create(
                    (int)ApplicationEnum.APIStatus.Success,
                    "KOT order history fetched successfully.",
                    result
                );
            }
            catch (Exception ex)
            {
                return Tuple.Create(
                    (int)ApplicationEnum.APIStatus.Failed,
                    ex.Message,
                    (ViewOrderHistory)null
                );
            }
        }

        public async Task<Tuple<int, string, ViewOrder>> ViewOrderAsync(string ticketNo)
        {
            using var connection = _context.CreateConnection();
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            try
            {
                string headerQuery = @"
                    SELECT DISTINCT 
                        RTRIM(TicketNo) AS TicketNo,
                        RTRIM(TableNo) AS TableNo,
                        RTRIM(KOT_Status) AS OrderStatus,
                        GrandTotal AS TotalAmount,
                        BillDate AS OrderDate
                    FROM RestaurantPOS_OrderInfoKOT
                    WHERE KOT_Status = 'Open' AND TicketNo = @TicketNo

                    UNION

                    SELECT DISTINCT 
                        RTRIM(BillNo) AS TicketNo,
                        'TakeAway' AS TableNo,
                        RTRIM(TA_Status) AS OrderStatus,
                        GrandTotal AS TotalAmount,
                        BillDate AS OrderDate
                    FROM RestaurantPOS_BillingInfoTA
                    WHERE TA_Status IN ('Unpaid', 'Paid Directly') AND BillNo = @TicketNo
                    ORDER BY 1;";

                var order = await connection.QueryFirstOrDefaultAsync<ViewOrder>(headerQuery, new { TicketNo = ticketNo });

                if (order == null)
                {
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, $"No order found for TicketNo '{ticketNo}'", (ViewOrder)null);
                }

                string detailQuery = @"
                    SELECT DISTINCT 
                        RTRIM(RestaurantPOS_OrderedProductKOT.Dish) AS Dish,
                        Quantity,
                        RTRIM(RestaurantPOS_OrderedProductKOT.Notes) AS Notes,
                        RTRIM(RestaurantPOS_OrderedProductKOT.Category) AS Category
                    FROM RestaurantPOS_OrderedProductKOT
                    INNER JOIN RestaurantPOS_OrderInfoKOT ON RestaurantPOS_OrderInfoKOT.ID = RestaurantPOS_OrderedProductKOT.TicketID
                    WHERE TicketNo = @TicketNo AND KOT_Status = 'Open'

                    UNION

                    SELECT DISTINCT 
                        RTRIM(RestaurantPOS_OrderedProductBillTA.Dish) AS Dish,
                        Quantity,
                        RTRIM(RestaurantPOS_OrderedProductBillTA.Notes) AS Notes,
                        RTRIM(RestaurantPOS_OrderedProductBillTA.Category) AS Category
                    FROM RestaurantPOS_OrderedProductBillTA
                    INNER JOIN RestaurantPOS_BillingInfoTA ON RestaurantPOS_BillingInfoTA.ID = RestaurantPOS_OrderedProductBillTA.BillID
                    WHERE BillNo = @TicketNo AND TA_Status IN ('Unpaid', 'Paid Directly');";

                var details = (await connection.QueryAsync<OrderDetails>(detailQuery, new { TicketNo = ticketNo })).ToList();

                order.Details = details;

                return Tuple.Create((int)ApplicationEnum.APIStatus.Success, "Order retrieved successfully", order);
            }
            catch (Exception ex)
            {
                return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, ex.Message, (ViewOrder)null);
            }
        }

        #region Edit Items

        public async Task<Tuple<int, string, OrderResponse>> UpdateOrderAsync(OrderMaster order, string originalOrderId, bool isTakeAway = false)
        {
            using var connection = _context.CreateConnection();
            if (connection.State == ConnectionState.Closed)
                connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1️⃣ Basic validation
                if (order.orderDetails == null || !order.orderDetails.Any())
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "No items added", (OrderResponse)null);

                if (!isTakeAway && string.IsNullOrWhiteSpace(order.Table))
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "Table not selected", (OrderResponse)null);

                // 2️⃣ Retrieve and store original order info before deletion
                var originalOrderInfo = await GetOriginalOrderInfo(connection, transaction, originalOrderId, isTakeAway);
                if (originalOrderInfo == null)
                    return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, "Original order not found", (OrderResponse)null);

                // 3️⃣ Revert original quantities (add back to stock)
                await RevertOriginalOrder(connection, transaction, originalOrderId, isTakeAway);

                // 4️⃣ Delete original order records
                await DeleteOriginalOrder(connection, transaction, originalOrderId, isTakeAway);

                // 5️⃣ Validate stock availability for new quantities
                var validationResult = await ValidateStockAvailability(connection, transaction, order.orderDetails);
                if (validationResult != null)
                    return validationResult;

                // 6️⃣ Create new order (reuse your existing insert methods)
                Tuple<int, string, OrderResponse> result;
                if (isTakeAway)
                {
                    result = await InsertTakeAwayOrderAsync(order);
                }
                else
                {
                    result = await InsertOrderAsync(order);
                }

                // If new order creation failed, rollback everything
                if (result.Item1 != (int)ApplicationEnum.APIStatus.Success)
                {
                    transaction.Rollback();
                    return result;
                }

                // 7️⃣ Commit transaction
                transaction.Commit();

                return Tuple.Create((int)ApplicationEnum.APIStatus.Success, "Order updated successfully", result.Item3);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Tuple.Create((int)ApplicationEnum.APIStatus.Failed, ex.Message, (OrderResponse)null);
            }
        }

        // Helper method to get original order information
        private async Task<dynamic> GetOriginalOrderInfo(IDbConnection connection, IDbTransaction transaction, string orderId, bool isTakeAway)
        {
            if (isTakeAway)
            {
                const string query = @"
            SELECT ID, BillNo, TableNo, Operator, BillDate
            FROM RestaurantPOS_BillingInfoTA 
            WHERE BillNo = @OrderId";

                return await connection.QueryFirstOrDefaultAsync(query, new { OrderId = orderId }, transaction);
            }
            else
            {
                const string query = @"
            SELECT ID, TicketNo, TableNo, Operator, BillDate
            FROM RestaurantPOS_OrderInfoKOT 
            WHERE TicketNo = @OrderId";

                return await connection.QueryFirstOrDefaultAsync(query, new { OrderId = orderId }, transaction);
            }
        }

        // Helper method to revert original quantities
        private async Task RevertOriginalOrder(IDbConnection connection, IDbTransaction transaction, string orderId, bool isTakeAway)
        {
            // Get original items first
            var originalItems = await GetOriginalOrderItems(connection, transaction, orderId, isTakeAway);

            if (originalItems == null || !originalItems.Any())
                return;

            // Revert finished goods stock
            const string revertStock = "UPDATE Temp_Stock_Store SET Qty = Qty + @Qty WHERE Dish = @Dish";

            foreach (var item in originalItems)
            {
                await connection.ExecuteAsync(revertStock, new { Dish = item.Dish, Qty = item.Quantity }, transaction);
            }

            // For takeaway orders, also revert raw materials
            if (isTakeAway)
            {
                await RevertRawMaterials(connection, transaction, originalItems);
            }

            // Revert table status for dine-in orders
            if (!isTakeAway)
            {
                var originalOrderInfo = await GetOriginalOrderInfo(connection, transaction, orderId, isTakeAway);
                if (originalOrderInfo != null)
                {
                    const string revertTable = "UPDATE R_Table SET BkColor = @Color WHERE TableNo = @TableNo";
                    await connection.ExecuteAsync(revertTable, new { Color = Color.Green.ToArgb(), TableNo = originalOrderInfo.TableNo }, transaction);
                }
            }
        }

        // Helper method to get original order items
        private async Task<List<OrderDetail>> GetOriginalOrderItems(IDbConnection connection, IDbTransaction transaction, string orderId, bool isTakeAway)
        {
            if (isTakeAway)
            {
                const string query = @"
                SELECT Dish, Quantity, Rate, Amount, DiscountPer, DiscountAmount, 
                       STPer, STAmount, VATPer, VATAmount, SCPer, SCAmount, 
                       Notes, Category, isComboDeal
                FROM RestaurantPOS_OrderedProductBillTA 
                WHERE BillID = (SELECT ID FROM RestaurantPOS_BillingInfoTA WHERE BillNo = @OrderId)";

                var result = await connection.QueryAsync<OrderDetail>(query, new { OrderId = orderId }, transaction);
                return result.AsList();
            }
            else
            {
                const string query = @"
                SELECT Dish, Quantity, Rate, Amount, DiscountPer, DiscountAmount, 
                       STPer, STAmount, VATPer, VATAmount, SCPer, SCAmount, 
                       Notes, Category, isComboDeal
                FROM RestaurantPOS_OrderedProductKOT 
                WHERE TicketID = (SELECT ID FROM RestaurantPOS_OrderInfoKOT WHERE TicketNo = @OrderId)";

                var result = await connection.QueryAsync<OrderDetail>(query, new { OrderId = orderId }, transaction);
                return result.AsList();
            }
        }

        // Helper method to revert raw materials for takeaway orders
        private async Task RevertRawMaterials(IDbConnection connection, IDbTransaction transaction, List<OrderDetail> originalItems)
        {
            const string recipeQuery = @"
            SELECT RJ.ProductID, RJ.Quantity
            FROM Recipe R
            INNER JOIN Recipe_Join RJ ON R.R_ID = RJ.RecipeID
            WHERE R.Dish = @Dish";

            foreach (var item in originalItems)
            {
                var recipeItems = await connection.QueryAsync(recipeQuery, new { Dish = item.Dish }, transaction);

                foreach (var rm in recipeItems)
                {
                    decimal revertedQty = (decimal)rm.Quantity * item.Quantity;
                    const string revertRMStock = "UPDATE Temp_Stock_RM SET Qty = Qty + @Qty WHERE ProductID = @ProductID";
                    await connection.ExecuteAsync(revertRMStock, new { Qty = revertedQty, ProductID = rm.ProductID }, transaction);
                }
            }
        }

        // Helper method to delete original order
        private async Task DeleteOriginalOrder(IDbConnection connection, IDbTransaction transaction, string orderId, bool isTakeAway)
        {
            if (isTakeAway)
            {
                // Delete from related tables first
                const string deleteOrderedItems = @"
                DELETE FROM RestaurantPOS_OrderedProductBillTA 
                WHERE BillID = (SELECT ID FROM RestaurantPOS_BillingInfoTA WHERE BillNo = @OrderId)";

                const string deleteBillingInfo = "DELETE FROM RestaurantPOS_BillingInfoTA WHERE BillNo = @OrderId";

                const string deleteRMUsedJoin = @"
                DELETE FROM RM_Used_Join 
                WHERE RawMaterialID = (SELECT RM_ID FROM RM_Used WHERE BillNo = @OrderId)";

                const string deleteRMUsed = "DELETE FROM RM_Used WHERE BillNo = @OrderId";

                const string deleteTblOrder = "DELETE FROM tblOrder WHERE BillNo = @OrderId";

                await connection.ExecuteAsync(deleteOrderedItems, new { OrderId = orderId }, transaction);
                await connection.ExecuteAsync(deleteRMUsedJoin, new { OrderId = orderId }, transaction);
                await connection.ExecuteAsync(deleteRMUsed, new { OrderId = orderId }, transaction);
                await connection.ExecuteAsync(deleteTblOrder, new { OrderId = orderId }, transaction);
                await connection.ExecuteAsync(deleteBillingInfo, new { OrderId = orderId }, transaction);
            }
            else
            {
                // Delete dine-in order
                const string deleteOrderedItems = @"
                DELETE FROM RestaurantPOS_OrderedProductKOT 
                WHERE TicketID = (SELECT ID FROM RestaurantPOS_OrderInfoKOT WHERE TicketNo = @OrderId)";

                const string deleteOrderInfo = "DELETE FROM RestaurantPOS_OrderInfoKOT WHERE TicketNo = @OrderId";

                await connection.ExecuteAsync(deleteOrderedItems, new { OrderId = orderId }, transaction);
                await connection.ExecuteAsync(deleteOrderInfo, new { OrderId = orderId }, transaction);
            }
        }

        // Helper method to validate stock availability
        private async Task<Tuple<int, string, OrderResponse>> ValidateStockAvailability(IDbConnection connection, IDbTransaction transaction, List<OrderDetail> orderDetails)
        {
            foreach (var group in orderDetails.GroupBy(o => o.Dish))
            {
                const string sqlQty = "SELECT Qty FROM Temp_Stock_Store WHERE Dish = @Dish";
                decimal availableQty = await connection.ExecuteScalarAsync<decimal?>(
                    sqlQty,
                    new { Dish = group.Key },
                    transaction: transaction
                ) ?? 0;

                decimal requestedQty = group.Sum(x => x.Quantity);

                // Uncomment if you want to enforce stock validation
                // if (requestedQty > availableQty)
                // {
                //     return Tuple.Create(
                //         (int)ApplicationEnum.APIStatus.Failed,
                //         $"Added qty ({requestedQty}) more than available ({availableQty}) for '{group.Key}'",
                //         (OrderResponse)null);
                // }
            }

            return null; // Validation passed
        }

        #endregion
    }
}