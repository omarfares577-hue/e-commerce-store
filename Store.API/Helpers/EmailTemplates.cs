using Store.Domain.Entities;
using System;
using System.Text;


namespace Store.API.Helpers
{
    public static class EmailTemplates
    {
        public static string GetOrderConfirmationTemplate(Order order, string customerName = null!)
        {
            var itemsTableRows = new StringBuilder();
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    var subtotal = item.Price * item.Quantity;
                    itemsTableRows.Append($@"
                    <tr style='border-bottom: 1px solid #eee;'>
                        <td style='padding: 10px; text-align: left;'>{item.ItemOrdered.ProductName}</td>
                        <td style='padding: 10px; text-align: center;'>${item.Price:F2}</td>
                        <td style='padding: 10px; text-align: center;'>{item.Quantity}</td>
                        <td style='padding: 10px; text-align: right;'>${subtotal:F2}</td>
                    </tr>");
                }
            }

            var address = order.ShippingAddress;
            string formattedAddress = address != null
                ? $"{address.FirstName} {address.LastName}, {address.Street}, {address.City}, {address.State}"
                : "N/A";

            string displayName = !string.IsNullOrEmpty(customerName)
                ? customerName
                : (address != null ? $"{address.FirstName} {address.LastName}" : order.BuyerEmail);

            return $@"
        <div style='font-family: Arial, sans-serif; text-align: left; direction: ltr; background-color: #f9f9f9; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 25px; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.05);'>
                <h2 style='color: #2c3e50; margin-top: 0;'>Thank you for your order, {displayName}!</h2>
                <p style='color: #555; font-size: 15px;'>Your order <b>#{order.Id}</b> has been placed successfully on <b>{order.OrderDate.ToString("yyyy-MM-dd HH:mm")} UTC</b>.</p>
                
                <!-- تفاصيل الشحن والعميل -->
                <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; border: 1px solid #e9ecef; margin: 15px 0;'>
                    <h4 style='margin: 0 0 10px 0; color: #333;'>Shipping & Customer Details:</h4>
                    <p style='margin: 3px 0; color: #555;'><b>Email:</b> {order.BuyerEmail}</p>
                    <p style='margin: 3px 0; color: #555;'><b>Address:</b> {formattedAddress}</p>
                </div>

                <!-- جدول المنتجات -->
                <h4 style='color: #333; margin-bottom: 10px;'>Order Summary:</h4>
                <table style='width: 100%; border-collapse: collapse; font-size: 14px; color: #333; margin-bottom: 20px;'>
                    <thead>
                        <tr style='background-color: #f1f1f1; text-align: left;'>
                            <th style='padding: 10px;'>Product</th>
                            <th style='padding: 10px; text-align: center;'>Price</th>
                            <th style='padding: 10px; text-align: center;'>Qty</th>
                            <th style='padding: 10px; text-align: right;'>Subtotal</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemsTableRows}
                    </tbody>
                </table>

                <!-- إجمالي الطلب -->
                <div style='text-align: right; font-size: 16px; font-weight: bold; color: #2c3e50; margin-top: 15px;'>
                    <p style='margin: 5px 0;'>Subtotal: ${order.Subtotal:F2}</p>
                    <p style='margin: 5px 0;'>Delivery Fee: ${(order.DeliveryMethod?.Cost ?? 0):F2}</p>
                    <h3 style='margin: 10px 0 0 0; color: #27ae60;'>Total Paid: ${order.GetTotal():F2}</h3>
                </div>

                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                <p style='font-size: 13px; color: #888; text-align: center;'>If you have any questions, feel free to contact our support team.</p>
            </div>
        </div>";
        }

        public static string GetAdminOrderAlertTemplate(Order order)
        {
            var itemsTableRows = new StringBuilder();
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    itemsTableRows.Append($@"
                    <tr style='border-bottom: 1px solid #eee;'>
                        <td style='padding: 8px;'>{item.ItemOrdered.ProductName}</td>
                        <td style='padding: 8px; text-align: center;'>{item.Quantity}</td>
                        <td style='padding: 8px; text-align: right;'>${(item.Price * item.Quantity):F2}</td>
                    </tr>");
                }
            }

            var address = order.ShippingAddress;
            string formattedAddress = address != null
                ? $"{address.FirstName} {address.LastName}, {address.Street}, {address.City}, {address.State}"
                : "N/A";

            return $@"
        <div style='font-family: Arial, sans-serif; direction: ltr; text-align: left; background-color: #fff3f3; padding: 20px;'>
            <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 25px; border-radius: 8px; border: 1px solid #ffcccc;'>
                <h2 style='color: #c0392b; margin-top: 0;'>🚨 Alert: New Order #{order.Id}</h2>
                <p style='color: #333;'>A new order was placed on <b>{order.OrderDate.ToString("yyyy-MM-dd HH:mm")} UTC</b>.</p>
                
                <p><b>Customer Email:</b> {order.BuyerEmail}</p>
                <p><b>Shipping Address:</b> {formattedAddress}</p>
                
                <h4>Items Ordered:</h4>
                <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <thead>
                        <tr style='background-color: #f8d7da; text-align: left;'>
                            <th style='padding: 8px;'>Product</th>
                            <th style='padding: 8px; text-align: center;'>Qty</th>
                            <th style='padding: 8px; text-align: right;'>Price</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemsTableRows}
                    </tbody>
                </table>

                <h3 style='text-align: right; color: #c0392b; margin-top: 15px;'>Total: ${order.GetTotal():F2}</h3>
                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                <p style='font-size: 12px; color: #666;'>Please check the admin dashboard to process this order.</p>
            </div>
        </div>";
        }
        public static string GetAccountBannedEmailTemplate(string displayName, string reactivationLink)
        {
            return $@"
        <div style='font-family: Arial, sans-serif; padding: 20px; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; border-radius: 8px;'>
            <h2 style='color: #d9534f;'>⚠️ Security Alert: Account Permanently Banned</h2>
            <p>Hello <strong>{displayName}</strong>,</p>
            <p>We detected multiple failed login attempts on your account, which has led to a permanent security ban to protect your data.</p>
            <p>If this was you and you wish to regain access to your account, you can reactivate it by setting a new password using the button below:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{reactivationLink}' style='background-color: #0275d8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reactivate Account</a>
            </div>
            <p style='color: #777; font-size: 12px;'>If you did not attempt to log in, please ignore this email and secure your account immediately.</p>

        </div>";
        }
        public static string GetForgotPasswordEmailTemplate(string displayName, string resetLink)
        {
            return $@"
        <div style='font-family: Arial, sans-serif; padding: 20px; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; border-radius: 8px;'>
            <h2 style='color: #0275d8;'>🔒 Reset Your Password</h2>
            <p>Hello <strong>{displayName}</strong>,</p>
            <p>We received a request to reset your password. You can set a new password by clicking the button below:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}' style='background-color: #0275d8; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reset Password</a>
            </div>
            <p style='color: #777; font-size: 12px;'>If you did not request a password reset, please ignore this email and your password will remain unchanged.</p>
        </div>";
        }
    }
}