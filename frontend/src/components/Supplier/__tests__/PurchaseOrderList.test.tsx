import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { vi, describe, it, expect, beforeEach } from "vitest";
import PurchaseOrderList from "../PurchaseOrderList";
import { supplierApi } from "../../../services/supplierApi";
import { PurchaseOrder } from "../../../types";
import { AxiosResponse } from "axios";

// Mock the supplier API
vi.mock("../../../services/supplierApi", () => ({
  supplierApi: {
    getPurchaseOrders: vi.fn(),
  },
}));

// Mock the PurchaseOrderModal component
vi.mock("../PurchaseOrderModal", () => ({
  default: ({
    purchaseOrder,
    onClose,
    onUpdate,
  }: {
    purchaseOrder: { purchaseOrderNumber: string };
    onClose: () => void;
    onUpdate: () => void;
  }) => (
    <div data-testid="purchase-order-modal">
      <p>Modal for {purchaseOrder.purchaseOrderNumber}</p>
      <button onClick={onClose}>Close</button>
      <button onClick={() => onUpdate()}>Update</button>
    </div>
  ),
}));

const mockPurchaseOrders: PurchaseOrder[] = [
  {
    id: "1",
    purchaseOrderNumber: "PO-001",
    customerOrderId: "co-1",
    supplierId: "supplier-1",
    status: "SentToSupplier",
    requiredDeliveryDate: "2024-01-15",
    items: [
      {
        id: "item-1",
        purchaseOrderId: "1",
        orderItemId: "oi-1",
        allocatedQuantity: 10,
      },
    ],
    createdAt: "2024-01-01",
    createdBy: "planner-1",
  },
  {
    id: "2",
    purchaseOrderNumber: "PO-002",
    customerOrderId: "co-2",
    supplierId: "supplier-1",
    status: "Confirmed",
    requiredDeliveryDate: "2024-01-20",
    items: [
      {
        id: "item-2",
        purchaseOrderId: "2",
        orderItemId: "oi-2",
        allocatedQuantity: 5,
      },
    ],
    createdAt: "2024-01-02",
    createdBy: "planner-1",
  },
  {
    id: "3",
    purchaseOrderNumber: "PO-003",
    customerOrderId: "co-3",
    supplierId: "supplier-1",
    status: "InProduction",
    requiredDeliveryDate: "2024-01-25",
    items: [
      {
        id: "item-3",
        purchaseOrderId: "3",
        orderItemId: "oi-3",
        allocatedQuantity: 15,
      },
    ],
    createdAt: "2024-01-03",
    createdBy: "planner-1",
  },
];

const createMockResponse = <T,>(data: T): AxiosResponse<T> => ({
  data,
  status: 200,
  statusText: "OK",
  headers: {},
  config: {} as any,
});

describe("PurchaseOrderList", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders loading state initially", () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockImplementation(
      () => new Promise(() => {})
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    expect(screen.getByText("Loading...")).toBeInTheDocument();
  });

  it("renders purchase orders list successfully", async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue(
      createMockResponse(mockPurchaseOrders)
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText("PO-001")).toBeInTheDocument();
      expect(screen.getByText("PO-002")).toBeInTheDocument();
      expect(screen.getByText("PO-003")).toBeInTheDocument();
    });

    expect(screen.getByText("Showing 3 of 3 orders")).toBeInTheDocument();
  });

  it("filters orders by status", async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue(
      createMockResponse(mockPurchaseOrders)
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText("PO-001")).toBeInTheDocument();
    });

    // Filter by Confirmed status
    const statusFilter = screen.getByDisplayValue("All Orders");
    fireEvent.change(statusFilter, { target: { value: "Confirmed" } });

    await waitFor(() => {
      expect(screen.getByText("Showing 1 of 3 orders")).toBeInTheDocument();
      expect(screen.getByText("PO-002")).toBeInTheDocument();
      expect(screen.queryByText("PO-001")).not.toBeInTheDocument();
      expect(screen.queryByText("PO-003")).not.toBeInTheDocument();
    });
  });

  it("opens purchase order modal when View Details is clicked", async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue(
      createMockResponse(mockPurchaseOrders)
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText("PO-001")).toBeInTheDocument();
    });

    const viewDetailsButton = screen.getAllByText("View Details")[0];
    fireEvent.click(viewDetailsButton);

    expect(screen.getByTestId("purchase-order-modal")).toBeInTheDocument();
    expect(screen.getByText("Modal for PO-001")).toBeInTheDocument();
  });

  it("updates order when modal triggers update", async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue(
      createMockResponse(mockPurchaseOrders)
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText("PO-001")).toBeInTheDocument();
    });

    // Open modal
    const viewDetailsButton = screen.getAllByText("View Details")[0];
    fireEvent.click(viewDetailsButton);

    // Trigger update
    const updateButton = screen.getByText("Update");
    fireEvent.click(updateButton);

    // Modal should close
    expect(
      screen.queryByTestId("purchase-order-modal")
    ).not.toBeInTheDocument();
  });

  it("displays correct status colors", async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue(
      createMockResponse(mockPurchaseOrders)
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    await waitFor(() => {
      const statusElements = screen.getAllByText("SentToSupplier");
      const sentToSupplierStatus = statusElements.find((el) =>
        el.closest("span")?.classList.contains("text-orange-800")
      );

      const confirmedElements = screen.getAllByText("Confirmed");
      const confirmedStatus = confirmedElements.find((el) =>
        el.closest("span")?.classList.contains("text-green-800")
      );

      const inProductionStatus = screen.getByText("InProduction");

      expect(sentToSupplierStatus?.closest("span")).toHaveClass(
        "text-orange-800"
      );
      expect(confirmedStatus?.closest("span")).toHaveClass("text-green-800");
      expect(inProductionStatus.closest("span")).toHaveClass("text-blue-800");
    });
  });

  it("shows confirm button only for pending orders", async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue(
      createMockResponse(mockPurchaseOrders)
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText("PO-001")).toBeInTheDocument();
    });

    // Should have confirm button for SentToSupplier status
    const confirmButtons = screen.getAllByText("Confirm");
    expect(confirmButtons).toHaveLength(1);
  });

  it("handles API errors gracefully", async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockRejectedValue(
      new Error("API Error")
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    await waitFor(() => {
      expect(
        screen.getByText("Failed to load purchase orders")
      ).toBeInTheDocument();
    });
  });

  it("displays empty state when no orders exist", async () => {
    vi.mocked(supplierApi.getPurchaseOrders).mockResolvedValue(
      createMockResponse([])
    );

    render(<PurchaseOrderList supplierId="supplier-1" />);

    await waitFor(() => {
      expect(screen.getByText("No purchase orders found.")).toBeInTheDocument();
    });
  });
});
