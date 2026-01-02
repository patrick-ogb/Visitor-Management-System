namespace VMS.Core.Enums;

public enum NotificationDeliveryStatus
{
    Pending = 0,    // Not yet delivered, user offline
    Delivered = 1,  // Sent via SignalR
    Failed = 2      // Delivery failed, will retry
}





