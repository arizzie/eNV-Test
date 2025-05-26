"use client";
import {
  Pagination,
  Spinner,
  Table,
  TableHeader,
  TableColumn,
  TableBody,
  TableRow,
  TableCell,
  getKeyValue,
  NumberInput,
  SortDescriptor,
  Input,
  DatePicker,
  Button,
  CalendarDate,
  useDisclosure,
  Snippet
} from "@heroui/react";
import { XCircleIcon } from "@heroicons/react/20/solid";
import React, { useState } from "react";
import useSWR from "swr";
import VehicleData from "../components/vehicledata";
import LoadCsv from "../components/loadcsv";

interface GetVinsAPI {
  TotalCount: number;
  Items: {
    Vin: string;
    DealerId: string;
    ModifiedDate: Date;
  }[];
}

async function fetcher(url: string) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(response.statusText);
  }

  const res = (await response.json()) as GetVinsAPI;

  return res;
}

const defaultPageSize = 10;
const defaultPage = 1;

const columns = [
  { name: "Vin", uid: "vin", sortable: true },
  { name: "Dealer Id", uid: "dealerId", sortable: true },
  { name: "Modified Date", uid: "modifiedDate", sortable: true },
  { name: "", uid: "actions", sortable: false },
];

export default function Home() {
  const [page, setPage] = useState(defaultPage);
  const [pageSize, setPageSize] = useState(defaultPageSize);
  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>({
    column: "vin",
    direction: "ascending",
  });

  const [modifiedDate, setModifiedDate] = useState<CalendarDate | null>(null);
  const [dealerId, setDealerId] = useState<string>("");
  const [vin, setVin] = useState<string>("");
  const detailsModal = useDisclosure();
  const importModal = useDisclosure();

  const url = `http://localhost:7124/api/vins?pageNumber=${page}&pageSize=${pageSize}&sort=${
    sortDescriptor.column
  }&direction=${sortDescriptor.direction}&modifiedDate=${
    modifiedDate?.toString() ?? ""
  }&dealerId=${dealerId}`;

  const { data, error, isLoading } = useSWR(url, fetcher, {
    keepPreviousData: true,
  });

  const pages = React.useMemo(() => {
    return data?.TotalCount ? Math.ceil(data.TotalCount / pageSize) : 0;
  }, [data?.TotalCount, pageSize]);

   const loadingState = isLoading ? "loading" : "idle";

  const clearModifiedDate = (e) => {
    setModifiedDate(null);
  };

  const openVehicleDataModal = (vin: string) => {
    setVin(vin);
    detailsModal.onOpen();
  };

  const closeVehicleDataModal = () => {
    setVin("");
    detailsModal.onOpenChange();
  };
  const closeImportDataModal = () => {
    importModal.onOpenChange();
  };
  const openImportDataModal = () => {
    importModal.onOpen();
  };

  return (
    <div className="container mx-auto p-6 w-4/5">
      {/* 1. Import Data Button */}
      <div className="mb-10 flex justify-end">
        {" "}
        {/* Changed justify-start to justify-end */}
        <Button
          onPress={openImportDataModal}
          color="primary"
          variant="bordered"
        >
          Import Data
        </Button>
      </div>
      {/* 2. Filters (Inline at top of table) */}
      <div className="flex sm:flex-row gap-4 mb-6 w-full justify-start items-center">
        <Input
          type="text" // Keep it as type="text"
          isClearable
          label="Search Dealer"
          value={dealerId}
          onValueChange={(value) => {
            const numericValue = value.replace(/[^0-9]/g, "");
            setDealerId(numericValue);
          }}
          placeholder="Search"
          className="w-full sm:w-auto flex-grow"
          labelPlacement="outside"
          inputMode="numeric"
          pattern="[0-9]*"
        />
        <DatePicker
          selectorButtonPlacement="start"
          label="Modified Date"
          value={modifiedDate}
          onChange={setModifiedDate}
          endContent={
            modifiedDate ? (
              <Button
                size="sm"
                isIconOnly
                color="danger"
                aria-label="Clear date"
                variant="light"
                onPress={clearModifiedDate}
              >
                <XCircleIcon className="h-4 w-4" />{" "}
                {/* Make sure XCircleIcon is imported */}
              </Button>
            ) : null
          }
          className="w-full sm:w-auto flex-grow"
          labelPlacement="outside"
        />
      </div>
      {/* 3. Table Component */}
      <Table
        sortDescriptor={sortDescriptor}
        onSortChange={setSortDescriptor}
        bottomContent={
          pages > 0 ? (
            <div className="flex w-full items-center justify-between p-4">
              <div className="flex-none">
                <NumberInput
                  onValueChange={setPageSize}
                  value={pageSize}
                  label="Page size"
                  minValue={1}
                  defaultValue={defaultPageSize}
                  className="w-40"
                  labelPlacement="outside-left"
                />
              </div>
              <div className="flex-grow flex justify-center">
                <Pagination
                  isCompact
                  showControls
                  showShadow
                  color="primary"
                  page={page}
                  total={pages}
                  onChange={(page) => setPage(page)}
                />
              </div>
              <div className="flex-none w-32 hidden sm:block"></div>
            </div>
          ) : null
        }
      >
        <TableHeader columns={columns}>
          {(column) => (
            <TableColumn key={column.uid} allowsSorting={column.sortable}>
              {column.name}
            </TableColumn>
          )}
        </TableHeader>
        <TableBody
          items={error ? [] : data?.Items ?? []}
          loadingContent={<Spinner />}
          loadingState={loadingState}
          emptyContent="No data found"
        >
          {(item) => (
            <TableRow key={item.Vin}>
              <TableCell>
                <Snippet hideSymbol variant="bordered">                
                  <span className="font-semibold text-gray-800">
                  {getKeyValue(item, "Vin")}
                </span>
                </Snippet>

              </TableCell>
              <TableCell>{getKeyValue(item, "DealerId")}</TableCell>
              <TableCell>
                {new Date(
                  getKeyValue(item, "ModifiedDate")
                ).toLocaleDateString()}
              </TableCell>
              <TableCell>
                <div className="flex justify-center">
                  <Button
                    onPress={() => {
                      openVehicleDataModal(item.Vin);
                    }}
                    size="sm"
                    variant="flat"
                    color="primary"
                    className="px-3"
                  >
                    Details
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
      {/* Modals remain outside the main layout flow */}
      <VehicleData
        vin={vin}
        isOpen={detailsModal.isOpen}
        onClose={closeVehicleDataModal}
      />
      <LoadCsv isOpen={importModal.isOpen} onClose={closeImportDataModal} />
    </div>
  );
}
