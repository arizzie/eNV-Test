'use client';
import { Pagination, Spinner,Table, TableHeader, TableColumn, 
  TableBody, TableRow, TableCell, getKeyValue, NumberInput,
  SortDescriptor, 
  Input,
  DatePicker,
  Button,
  CalendarDate,
  useDisclosure
} from "@heroui/react";
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
};

async function fetcher(url: string) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(response.statusText);
  }

  const res = await response.json() as GetVinsAPI;

  return res;
}

const defaultPageSize = 10;
const defaultPage = 1;

const columns = [
  { name: "Vin", uid: "vin", sortable: true },
  { name: "Dealer Id", uid: "dealerId", sortable: true },
  { name: "Modified Date", uid: "modifiedDate", sortable: true },
];


export default function Home() {

  const [page, setPage] = useState(defaultPage);
  const [pageSize, setPageSize] = useState(defaultPageSize);
  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>({
    column: "vin",
    direction: "ascending",
  });

  const [modifiedDate, setModifiedDate] = useState <CalendarDate | null>(null);
  const [dealerId, setDealerId] = useState<string>("");
  const [vin, setVin] = useState<string>("");
   const detailsModal = useDisclosure();
   const importModal = useDisclosure();

  const url = `http://localhost:7124/api/vins?pageNumber=${page}&pageSize=${pageSize}&sort=${sortDescriptor.column}&direction=${sortDescriptor.direction}&modifiedDate=${modifiedDate?.toString() ?? ""}&dealerId=${dealerId}`;


  const {data, error, isLoading} = useSWR(url, fetcher, {
    keepPreviousData: true,
  });


  const pages = React.useMemo(() => {
    return data?.TotalCount ? Math.ceil(data.TotalCount / pageSize) : 0;
  }, [data?.TotalCount, pageSize]);

  const loadingState = isLoading || data?.TotalCount === 0 ? "loading" : "idle";

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
    <div className="container mx-auto p-4">
      <Button onPress={openImportDataModal} color="primary" variant="bordered">Import Data</Button>
      <Input type="text" isClearable label="Search Dealer" value={dealerId} onValueChange={setDealerId} placeholder="Search" />
      <DatePicker selectorButtonPlacement="start" label="Modified Date" 
      value={modifiedDate}
      onChange={setModifiedDate}
      endContent=
      {
        modifiedDate ?
        <Button
          size="sm"
          isIconOnly
          color="danger"
          aria-label="Clear"
          variant="light"
          onPress={clearModifiedDate}
        >
          X
        </Button>
        : null
      } />
      <Table 
      sortDescriptor={sortDescriptor}
      onSortChange={setSortDescriptor}
      bottomContent={
        pages > 0 ? (
          <div className="flex w-full justify-center">
            <NumberInput
              onValueChange={setPageSize}
              value={pageSize}
              label="Page size"
              minValue={1}
              defaultValue={defaultPageSize}
            />
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
        ) : null
      }
    >
      <TableHeader columns={columns}>
          {(column) => (
            <TableColumn
              key={column.uid}
              allowsSorting={column.sortable}
            >
              {column.name}
            </TableColumn>
          )}
        </TableHeader>
     <TableBody items={error ? [] :data?.Items ?? []}
        loadingContent={<Spinner />}
        loadingState={loadingState}
        emptyContent="No data found"
        >
        {(item) => 
          <TableRow key={item.Vin}>
            <TableCell>
              {getKeyValue(item, "Vin")}
              <Button onPress={() => { openVehicleDataModal(item.Vin)}}>Details</Button>  
            </TableCell>
            <TableCell>{getKeyValue(item, "DealerId")}</TableCell>
            <TableCell>{new Date(getKeyValue(item, "ModifiedDate")).toLocaleDateString()}</TableCell>
          </TableRow>
        }
      </TableBody>
    </Table>
    <VehicleData vin={vin} isOpen={detailsModal.isOpen} onClose={closeVehicleDataModal}/>
    <LoadCsv isOpen={importModal.isOpen} onClose={closeImportDataModal}/>
    </div>
  );
}
