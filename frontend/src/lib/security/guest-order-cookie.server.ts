import { cookies } from "next/headers";
import { guestOrderCookieName } from "./guest-order-cookie";

export async function readGuestOrderToken(
  orderId: string,
): Promise<string | undefined> {
  const jar = await cookies();
  const value = jar.get(guestOrderCookieName(orderId))?.value;
  return value && value.length > 0 ? value : undefined;
}
